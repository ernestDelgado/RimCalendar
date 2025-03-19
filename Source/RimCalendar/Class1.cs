using RimWorld;
using System;
using UnityEngine;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;

namespace RimCalendar
{
    [StaticConstructorOnStartup]
    public class RimCalendarUI : GameComponent
    {

        private static Vector2[] xPositions = new Vector2[]
        {
            new Vector2(3f, 53f), //day 1
            new Vector2(28f, 53f), //day 2
            new Vector2(53f, 53f), //day 3
            new Vector2(78f, 53f), //day 4
            new Vector2(103f, 53f), //day 5
            new Vector2(3f, 77f), //day 6
            new Vector2(28f, 77f), //day 7
            new Vector2(53f, 77f), //day 8
            new Vector2(78f, 77f), //day 9
            new Vector2(103f, 77f), //day 10
            new Vector2(3f, 101f), //day 11
            new Vector2(28f, 101f), //day 12
            new Vector2(53f, 101f), //day 13
            new Vector2(78f, 101f),  //day 14
            new Vector2(103f, 101f)  //day 15
        };
        private static Texture2D xImage;
        private static Texture2D blueXImage;
        private static Texture2D redXImage;

        private static Texture2D calendarSummerImage;
        private static Texture2D calendarFallImage;
        private static Texture2D calendarWinterImage;
        private static Texture2D calendarSpringImage;
        private static Texture2D currentCalendarImage;
        private static string lastSeason = "";

      
        private static string lastMonth = "";

        
        private static string lastYear = "";
        private static Dictionary<int, Texture2D> seasonXTextures = new Dictionary<int, Texture2D>(); // Stores X marks per season
        private static int lastDayChecked = -1; // Tracks the last processed game day

        private static Texture2D birthdayCakeImage;
        private static Dictionary<int, List<Pawn>> birthdayPawns = new Dictionary<int, List<Pawn>>(); // Stores pawns with birthdays for each day
        private static Dictionary<int, List<Quest>> Events = new Dictionary<int, List<Quest>>(); // Stores events
        private static Dictionary<int, List<Pawn>> DeathAnniversaries = new Dictionary<int, List<Pawn>>();


        private static Texture2D drumImage;
        private static Texture2D refugeeImage;
        private static Texture2D shuttleImage;
        private static Texture2D questExpiredImage;
        private static Texture2D gravestoneImage;
        private static Texture2D raidImage;
        private static Texture2D radioImage;


        private static Harmony harmony;

        public RimCalendarUI(Game game) { }
        static RimCalendarUI()
        {
            harmony = new Harmony("com.rimcalendar.patch");

            //Load Mod Textures
            #region LoadImages
            xImage = ContentFinder<Texture2D>.Get("X", false);
            blueXImage = ContentFinder<Texture2D>.Get("BlueX", false);
            redXImage = ContentFinder<Texture2D>.Get("RedX", false);

            calendarSummerImage = ContentFinder<Texture2D>.Get("SummerCalendar", false);
            calendarFallImage = ContentFinder<Texture2D>.Get("FallCalendar", false);
            calendarWinterImage = ContentFinder<Texture2D>.Get("WinterCalendar", false);
            calendarSpringImage = ContentFinder<Texture2D>.Get("SpringCalendar", false);

            birthdayCakeImage = ContentFinder<Texture2D>.Get("Cake", false);
            refugeeImage = ContentFinder<Texture2D>.Get("Tent", false);
            drumImage = ContentFinder<Texture2D>.Get("Drum", false);
            shuttleImage = ContentFinder<Texture2D>.Get("Shuttle", false);
            questExpiredImage = ContentFinder<Texture2D>.Get("QuestExpired", false);
            gravestoneImage = ContentFinder<Texture2D>.Get("Gravestone", false);
            raidImage = ContentFinder<Texture2D>.Get("Raid", false);
            radioImage = ContentFinder<Texture2D>.Get("Radio", false);
            #endregion 


            //Patch the Calendar Image into the right UI bar
            harmony.Patch(
                original: AccessTools.Method(typeof(GlobalControlsUtility), "DoTimespeedControls"),
                postfix: new HarmonyMethod(typeof(RimCalendarUI), nameof(InjectCalendarIntoUI))
            );
            harmony.PatchAll();

            HideGameDateTime();
        }


        //Main FUNCTIONS
        public static void InjectCalendarIntoUI(ref float curBaseY)
        {
            // ✅ Get the current season, month, year, and day
            string currentSeason = GetCurrentSeason();
            string currentMonth = GetCurrentMonth();
            int currentDay = GenDate.DayOfSeason(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);

            #region ONCE PER SEASON
            // ============================
            if (currentSeason != lastSeason)
            {
                lastSeason = currentSeason;
                seasonXTextures.Clear(); // ✅ Reset X marks for the new season

                for (int i = 0; i < 14; i++)
                {
                    seasonXTextures[i] = GetRandomX(); // ✅ Assign new random X marks
                }

                // ✅ Ensure the calendar updates immediately
                currentCalendarImage = GetSeasonCalendarImage(currentSeason);
            }
            #endregion

            #region ONCE PER MONTH
            // ============================
            if (currentMonth != lastMonth)
            {
                // Update calendar & season text
                currentCalendarImage = GetSeasonCalendarImage(currentSeason);

                lastMonth = currentMonth;
            }
            #endregion

            #region ✅ ONCE PER DAY
            // Run daily updates only if the day has changed
            if (currentDay != lastDayChecked)
            {
                lastDayChecked = currentDay; // ✅ Store the last processed day
                UpdateBirthdays();
                UpdateEvents();
            }



            #endregion


            // ✅ Ensure all textures are valid before drawing
            if (currentCalendarImage == null)
                return;

            #region ✅ DRAW UI ELEMENTS (EVERY FRAME)
            // ============================
            float panelX = UI.screenWidth - 130f;
            float imageSize = 125f;
            curBaseY -= imageSize * 1.1f;

            // ✅ 1. Draw the calendar image
            Widgets.DrawTextureFitted(new Rect(panelX, curBaseY, imageSize, imageSize), currentCalendarImage, 1.0f);

            // ✅ 2. Draw the season text
            DrawSeason(panelX,curBaseY,imageSize);

            // ✅ 3. Draw the month text
            DrawMonth(panelX,curBaseY,imageSize);

            // ✅ 4. Draw the year 
            DrawYear(panelX,curBaseY,imageSize);

            // ✅ 5. Draw the current in-game hour
            DrawGameHour(panelX, curBaseY, imageSize);
            #endregion

            #region ✅ DRAW SPECIAL ICONS (EVERY FRAME)
            // ✅ 6. Place Gravestone Icons
            DrawDeathAnniversaries(panelX, curBaseY, currentDay);

            // ✅ 7. Place birthday cake icons
            DrawBirthdayCakes(panelX, curBaseY, currentDay);

            // ✅ 8. Draw Quest Events (e.g., "Desperate Nomads")
            DrawQuestEvents(panelX, curBaseY, currentDay);

            // ✅ 9. Place X marks for passed days
            DrawXMarks(panelX, curBaseY, currentDay);

            #endregion

            curBaseY -= imageSize * 0.40f;
        }


        //HELPER FUNCTIONS
        //Getters 
        private static Texture2D GetRandomX()
        {
            float roll = UnityEngine.Random.value; // ✅ Random float between 0.0 and 1.0

            if (roll < 0.40f) return xImage;       // ✅ 40% chance
            if (roll < 0.80f) return blueXImage;   // ✅ 40% chance
            return redXImage;                      // ✅ 20% chance
        }
        private static string GetCurrentYear()
        {
            // Absolute game ticks
            int absoluteTicks = Find.TickManager.TicksAbs;

            // Longitude-based offset calculation (same logic as hour calculation)
            float longitude = Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x;
            int offsetTicks = Mathf.RoundToInt((longitude / 360f) * GenDate.TicksPerDay);

            // Apply the offset
            int adjustedTicks = absoluteTicks + offsetTicks;

            // Get the correct year considering map position
            int currentYear = GenDate.Year(adjustedTicks, 0f);

            return currentYear.ToString();
        }

        private static string GetCurrentMonth()
        {
            Quadrum quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);
            return quadrum.ToString();
        }
        private static string GetCurrentSeason()
        {
            Season season = GenDate.Season(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile));
            string seasonText = season.ToString();
            if (seasonText == "PermanentSummer") return "Summer";
            else if (seasonText == "PermanentWinter") return "Winter";
            else return seasonText; // ✅ Default behavior for normal biomes
        }



        private static string GetCurrentHour()
        {
            int absoluteTicks = Find.TickManager.TicksAbs;

            // ✅ Get raw hour and manually calculate minutes
            int rawHour = GenDate.HourOfDay(absoluteTicks, 0f);
            int rawMinutes = (absoluteTicks % GenDate.TicksPerHour) * 60 / GenDate.TicksPerHour;

            // ✅ Adjust for longitude-based time zones
            float longitude = Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x;
            int offset = Mathf.RoundToInt(longitude / 15f); // ✅ Every 15° shifts by 1 hour

            // ✅ Proper modulo operation to handle negative values correctly
            int hour = (rawHour + offset) % 24;
            if (hour < 0) hour += 24; // 🔥 Fix: Ensure hour stays within 0-23

            // ✅ Convert to 12-hour format with AM/PM
            string period = (hour >= 12) ? "PM" : "AM";
            int hour12 = (hour > 12) ? hour - 12 : (hour == 0 ? 12 : hour);

            // ✅ Format minutes with leading zero (e.g., "8:02 PM" instead of "8:2 PM")
            string formattedTime = $"{hour12}:{rawMinutes:D2} {period}";

            return formattedTime;
        }



        




        //Texture

        private static Texture2D GetSeasonCalendarImage(string season)
        {
            switch (season)
            {
                case "Spring": return calendarSpringImage;
                case "Summer": return calendarSummerImage;
                case "Fall": return calendarFallImage;
                case "Winter": return calendarWinterImage;
                default: return calendarSpringImage; // ✅ Default case
            }
        }
        private static (Texture2D, string) GetEventInfo(Quest quest)
        {
            if (quest == null)
            {
                return (questExpiredImage, "Quest Expires: "); // 🚨 Prevents crashes
            }

            // ✅ Check if quest is not yet accepted
            if (quest.State == QuestState.NotYetAccepted)
            {
                return (questExpiredImage, "Quest Expires: ");
            }

            // ✅ Loop through all QuestParts and determine event type
            foreach (QuestPart part in quest.PartsListForReading)
            {
                if (part is QuestPart_RandomRaid)
                    return (raidImage, "Raid Arrives: "); // 🚨 Incoming Raid

                if (part is QuestPart_ShuttleDelay || part is QuestPart_ShuttleLeaveDelay)
                    return (shuttleImage, "Shuttle Arrives: "); // 🚀 Shuttle Event

                if (part is QuestPart_AddQuestRefugeeDelayedReward)
                    return (refugeeImage, "Refugees Depart: "); // 🏠 Refugee Event

                if (part is QuestPart_SendShuttleAway)
                    return (shuttleImage, "Shuttle Departs: "); // 🛫 Shuttle Leaving

                if (part is QuestPart_Delay && quest.State == QuestState.Ongoing)
                    return (radioImage, "Event Expires: "); // 📻 Timed Event (e.g., detected loot camp)
            }

            // 🚨 Default Fallback
            return (questExpiredImage, "Quest Expires: ");
        }




        //Misc 
        private static void ShowBirthdayPawnSelectionMenu(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0) return;

            if (pawns.Count == 1)
            {
                // ✅ If only one pawn has a birthday, select them immediately
                CameraJumper.TryJumpAndSelect(pawns[0]);
            }
            else
            {
                // ✅ If multiple pawns have a birthday, show a selection menu
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach (Pawn pawn in pawns)
                {
                    options.Add(new FloatMenuOption(pawn.LabelShortCap, () => CameraJumper.TryJumpAndSelect(pawn)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        public static bool HideDefaultDateUI()
        {
            return false; // Returning false stops the original function from running
        }
        private static void OpenQuestWindow(Quest quest)
        {
            if (quest == null) return;

            // ✅ Open the Quests Tab
            Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests, false);

            // ✅ Ensure the quest is not hidden
            quest.hidden = false;

            // ✅ Open detailed quest dialog correctly (without named arguments)
            ((MainTabWindow_Quests)Find.MainTabsRoot.OpenTab.TabWindow).Select(quest);
        }
        public static void UpdateEvents()
        {
            Events.Clear();

            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest == null) continue;

                int eventTicks = -1;
                bool isRaidEvent = false;
                bool isShuttleEvent = false;
                bool isRefugeeEvent = false;

                // ✅ If the quest is ongoing, determine the exact event time
                if (quest.State == QuestState.Ongoing)
                {
                    foreach (QuestPart part in quest.PartsListForReading)
                    {
                        // ✅ Detect generic delayed events (e.g., timed objectives, arrivals, departures)
                        if (part is QuestPart_Delay delayPart)
                        {
                            eventTicks = Find.TickManager.TicksAbs + delayPart.TicksLeft;
                        }

                        // ✅ Detect raids (e.g., "Fighting for Profit")
                        if (part is QuestPart_RandomRaid)
                        {
                            eventTicks = Find.TickManager.TicksAbs +
                                         quest.PartsListForReading.OfType<QuestPart_Delay>()
                                             .FirstOrDefault()?.TicksLeft ?? 0;
                            isRaidEvent = true;
                        }

                        // ✅ Detect shuttle arrivals or departures
                        if (part is QuestPart_ShuttleDelay || part is QuestPart_ShuttleLeaveDelay || part is QuestPart_SendShuttleAway)
                        {
                            eventTicks = Find.TickManager.TicksAbs +
                                         quest.PartsListForReading.OfType<QuestPart_Delay>()
                                             .FirstOrDefault()?.TicksLeft ?? 0;
                            isShuttleEvent = true;
                        }

                        // ✅ Detect refugee-related events
                        if (part is QuestPart_AddQuestRefugeeDelayedReward)
                        {
                            eventTicks = Find.TickManager.TicksAbs +
                                         quest.PartsListForReading.OfType<QuestPart_Delay>()
                                             .FirstOrDefault()?.TicksLeft ?? 0;
                            isRefugeeEvent = true;
                        }
                    }
                }
                // ✅ If the quest is not yet accepted, track its expiration time
                else if (quest.State == QuestState.NotYetAccepted && quest.ticksUntilAcceptanceExpiry > 0)
                {
                    eventTicks = Find.TickManager.TicksAbs + quest.ticksUntilAcceptanceExpiry;
                }

                // ✅ Ensure a valid event time exists
                if (eventTicks == -1 || eventTicks <= Find.TickManager.TicksAbs)
                    continue;

                // ✅ Convert to correct day & quadrum
                Quadrum eventQuadrum = GenDate.Quadrum(eventTicks, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);
                int eventDay = GenDate.DayOfSeason(eventTicks, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x) + 1;
                Quadrum currentQuadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);

                // ✅ Only track events happening in this quadrum
                if (eventQuadrum == currentQuadrum)
                {
                    if (!Events.ContainsKey(eventDay))
                        Events[eventDay] = new List<Quest>();

                    // ✅ Categorizing quest types
                    string questType = quest.State == QuestState.NotYetAccepted ? "Available Quest"
                                    : isRaidEvent ? "Raid Event"
                                    : isShuttleEvent ? "Shuttle Event"
                                    : isRefugeeEvent ? "Refugee Event"
                                    : "Active Quest";

                    Log.Message($"RIMCALENDAR: {questType} '{quest.name}' occurs on {eventQuadrum} Day {eventDay}");

                    Events[eventDay].Add(quest);
                }
            }
        }
        public static void UpdateBirthdays()
        {
            birthdayPawns.Clear();
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn.ageTracker == null) continue;

                int birthDayOfYear = pawn.ageTracker.BirthDayOfYear;
                Quadrum birthQuadrum = (Quadrum)(birthDayOfYear / 15);
                int birthDay = (birthDayOfYear % 15) + 1;
                Quadrum currentQuadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);

                if (birthQuadrum == currentQuadrum)
                {
                    if (!birthdayPawns.ContainsKey(birthDay))
                        birthdayPawns[birthDay] = new List<Pawn>();

                    birthdayPawns[birthDay].Add(pawn);
                }
            }
        }

        public static void RegisterPawnDeath(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer) return; // Ignore non-colonists

            int deathDay = GenDate.DayOfSeason(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);
            Quadrum deathQuadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile).x);

            if (!DeathAnniversaries.ContainsKey(deathDay))
                DeathAnniversaries[deathDay] = new List<Pawn>();

            DeathAnniversaries[deathDay].Add(pawn); // Store pawn reference instead of just name
        }
        private static void JumpToGraveOrLog(List<Pawn> deadPawns)
        {
            foreach (Pawn pawn in deadPawns)
            {
                // ✅ Find the grave that contains this pawn
                Building_Grave grave = Find.CurrentMap.listerThings.ThingsOfDef(ThingDefOf.Grave)
                    .OfType<Building_Grave>()
                    .FirstOrDefault(g => g.Corpse?.InnerPawn == pawn);

                if (grave != null)
                {
                    CameraJumper.TryJump(new GlobalTargetInfo(grave.Position, Find.CurrentMap));
                    Messages.Message($"Jumping to {pawn.LabelShortCap}'s grave.", MessageTypeDefOf.PositiveEvent, false);
                    return; // ✅ Stop searching after finding the first grave
                }
            }

            // ❌ No grave found for any pawn
            Messages.Message("No known graves for this pawns.", MessageTypeDefOf.RejectInput, false);
        }
        private static void HideGameDateTime()
        {
            harmony.Patch(
                    original: AccessTools.Method(typeof(GlobalControlsUtility), "DoDate"),
                    prefix: new HarmonyMethod(typeof(RimCalendarUI), nameof(HideDefaultDateUI))
                );
        }


        //Draw 
        private static void DrawGameHour(float panelX, float curBaseY, float imageSize)
        {
            string hourText = GetCurrentHour(); // ✅ Use fixed game hour format

            Rect hourRect = new Rect(panelX, curBaseY - 55f, imageSize, 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Medium;
            Widgets.Label(hourRect, hourText);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private static void DrawSeason(float panelX, float curBaseY, float imageSize)
        {
            string seasonText = GetCurrentSeason(); // ✅ Use fixed game hour format

            Rect seasonRect = new Rect(panelX, curBaseY - 30f, imageSize, 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Medium;
            Widgets.Label(seasonRect, seasonText);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private static void DrawMonth(float panelX, float curBaseY, float imageSize)
        {
            string monthText = GetCurrentMonth(); // ✅ Use fixed game hour format

            Rect monthRect = new Rect(panelX, curBaseY + 20f, imageSize, 30f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(monthRect, monthText);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private static void DrawYear(float panelX, float curBaseY, float imageSize)
        {
            string yearText = GetCurrentYear(); // ✅ Use fixed game hour format

            Rect yearRect = new Rect(panelX - 5f, curBaseY, imageSize, 25f);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Medium;
            Widgets.Label(yearRect, yearText);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private static void DrawBirthdayCakes(float panelX, float curBaseY, int currentDay)
        {
            if (currentDay > 15) return;

            for (int i = 1; i <= 15; i++)
            {
                if (!birthdayPawns.ContainsKey(i) || i > xPositions.Length) continue;

                float cakeOffsetX = panelX + xPositions[i - 1].x;
                float cakeOffsetY = curBaseY + xPositions[i - 1].y;
                float cakeSize = 20f;

                Rect cakeRect = new Rect(cakeOffsetX, cakeOffsetY, cakeSize, cakeSize);
                Widgets.DrawTextureFitted(cakeRect, birthdayCakeImage, 1.0f);

                string birthdayTooltip = "🎂 Birthday: " + string.Join(", ", birthdayPawns[i].Select(p => p.NameShortColored.Resolve()));
                TooltipHandler.TipRegion(cakeRect, birthdayTooltip);

                if (Widgets.ButtonInvisible(cakeRect))
                {
                    ShowBirthdayPawnSelectionMenu(birthdayPawns[i]);
                }
            }
        }
        private static void DrawXMarks(float panelX, float curBaseY, int currentDay)
        {
            if (currentDay > 14) return;

            for (int i = 0; i < currentDay; i++)
            {
                if (!seasonXTextures.ContainsKey(i) || i >= xPositions.Length) continue;

                float xOffsetX = panelX + xPositions[i].x;
                float xOffsetY = curBaseY + xPositions[i].y;
                float xSize = 20f;

                Rect xRect = new Rect(xOffsetX, xOffsetY, xSize, xSize);
                Widgets.DrawTextureFitted(xRect, seasonXTextures[i], 1.0f);
            }
        }
        private static void DrawQuestEvents(float panelX, float curBaseY, int currentDay)
        {
            if (currentDay > 15) return; // ✅ Don't process beyond the calendar

            for (int i = 1; i <= 15; i++)
            {
                if (!Events.ContainsKey(i) || i > xPositions.Length) continue;

                float eventOffsetX = panelX + xPositions[i - 1].x;
                float eventOffsetY = curBaseY + xPositions[i - 1].y;
                float eventSize = 20f;

                Rect eventRect = new Rect(eventOffsetX, eventOffsetY, eventSize, eventSize);

                // ✅ Get the first quest on this day (for icon purposes)
                List<Quest> questsOnThisDay = Events[i];
                if (questsOnThisDay.Count == 0) continue;

                // ✅ Use the first quest's event info (since we can't fit multiple icons)
                (Texture2D eventImage, string eventText) = GetEventInfo(questsOnThisDay[0]);

                // ✅ Draw event icon
                Widgets.DrawTextureFitted(eventRect, eventImage, 1.0f);

                // ✅ Construct tooltip with all quest names
                string questTooltip = $"{eventText}{string.Join(", ", questsOnThisDay.Select(q => q.name))}";
                TooltipHandler.TipRegion(eventRect, questTooltip);

                // ✅ Clicking opens quest selection menu if multiple quests exist
                if (Widgets.ButtonInvisible(eventRect))
                {
                    if (questsOnThisDay.Count == 1)
                    {
                        OpenQuestWindow(questsOnThisDay[0]); // ✅ Open the quest window if only 1 quest
                    }
                    else
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Quest quest in questsOnThisDay)
                        {
                            options.Add(new FloatMenuOption(quest.name, () => OpenQuestWindow(quest)));
                        }
                        Find.WindowStack.Add(new FloatMenu(options)); // ✅ Show menu if multiple quests exist
                    }
                }
            }
        }

        private static void DrawDeathAnniversaries(float panelX, float curBaseY, int currentDay)
        {
            if (currentDay > 15) return;

            for (int i = 1; i <= 15; i++)
            {
                if (!DeathAnniversaries.ContainsKey(i) || i > xPositions.Length) continue;

                float eventOffsetX = panelX + xPositions[i - 1].x;
                float eventOffsetY = curBaseY + xPositions[i - 1].y;
                float eventSize = 20f;

                Rect eventRect = new Rect(eventOffsetX, eventOffsetY, eventSize, eventSize);
                Widgets.DrawTextureFitted(eventRect, gravestoneImage, 1.0f);

                string tooltipText = "🪦 Death Anniversary: " + string.Join(", ", DeathAnniversaries[i].Select(p => p.LabelShortCap));
                TooltipHandler.TipRegion(eventRect, tooltipText);

                if (Widgets.ButtonInvisible(eventRect))
                {
                    JumpToGraveOrLog(DeathAnniversaries[i]);
                }
            }
        }



        //UPDATE FUNCTION
        /*public override void GameComponentUpdate()
        {
            if (KeyBindingDef.Named("RimCalendar_ToggleR").JustPressed) // ✅ Detects R key
            {
                Log.Message("R KEY is Being pressed!");
                HideGameDateTime();

            }

            if (KeyBindingDef.Named("RimCalendar_ToggleT").JustPressed) // ✅ Detects T key
            {
                Log.Message("T KEY is Being pressed!");
                harmony.Unpatch(
                    AccessTools.Method(typeof(GlobalControlsUtility), "DoDate"),
                    HarmonyPatchType.Prefix
                );
            }
        }*/

    }

    [HarmonyPatch(typeof(QuestManager), "Add")]
    public static class Patch_QuestAdded
    {
        [HarmonyPostfix]
        public static void OnQuestAdded(QuestManager __instance)
        {
            Log.Message("Inside OnQuestAdded");

            // ✅ Get the last added quest (ensure we reference the latest one)
            Quest newQuest = __instance.QuestsListForReading.LastOrDefault();

            if (newQuest != null)
            {
                // ✅ Log the new quest details for debugging
                Log.Message($"[RimCalendar] New quest added: {newQuest.name} | State: {newQuest.State}");

                // ✅ Update RimCalendar events whenever ANY quest is added, regardless of its initial state
                RimCalendarUI.UpdateEvents();
            }
        }
    }


    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Pawn_Kill
    {
        public static void Postfix(Pawn __instance)
        {
            Log.Message("Inside Patch_Pawn_Kill");
            if (__instance != null && __instance.Faction == Faction.OfPlayer)
            {
                RimCalendarUI.RegisterPawnDeath(__instance);
            }
        }
    }

}

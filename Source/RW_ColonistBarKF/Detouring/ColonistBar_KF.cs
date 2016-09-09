﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using RimWorld;
using UnityEngine;
using Verse;
using static ColonistBarKF.CBKF;
using static ColonistBarKF.BarSettings.SortByWhat;

namespace ColonistBarKF
{
    [StaticConstructorOnStartup]
    public class ColonistBar_KF
    {

        private const float PawnTextureHorizontalPadding = 1f;

        private List<Pawn> cachedColonists = new List<Pawn>();

        private List<Vector2> cachedDrawLocs = new List<Vector2>();

        private bool colonistsDirty = true;

        private Dictionary<string, string> pawnLabelsCache = new Dictionary<string, string>();

        private Pawn clickedColonist;

        private float clickedAt;


        // custom test

        private static Vector2 BaseSize = new Vector2(CBKF.BarSettings.BaseSizeFloat, CBKF.BarSettings.BaseSizeFloat);

        //      public static readonly Vector2 PawnTextureSize = new Vector2(BaseSize.x - 2f, 75f);
        public static Vector2 PawnTextureSize = new Vector2(CBKF.BarSettings.BaseSizeFloat - 2f, CBKF.BarSettings.BaseSizeFloat * 1.5f);

        private static Vector3 PawnTextureCameraOffset = new Vector3(0f, 0f, 0.3f);

        private float Scale
        {
            get
            {
                float num = 1f;

                if (CBKF.BarSettings.UseFixedIconScale)
                {
                    return 1f;
                }

                if (CBKF.BarSettings.UseVerticalAlignment)
                {
                    while (true)
                    {
                        int allowedColumnsCountForScale = GetAllowedColumnsCountForScale(num);
                        int num2 = ColumnsCountAssumingScale(num);
                        if (num2 <= allowedColumnsCountForScale)
                        {
                            break;
                        }
                        num *= 0.95f;
                    }
                    return num;
                }

                if (CBKF.BarSettings.UseCustomIconSize)
                {

                    while (true)
                    {
                        int allowedRowsCountForScale = GetAllowedRowsCountForScaleModded(num);
                        int num2 = RowsCountAssumingScale(num);
                        if (num2 <= allowedRowsCountForScale)
                        {
                            break;
                        }
                        num *= 0.95f;
                    }
                    return num;
                }


                while (true)
                {
                    int allowedRowsCountForScale = GetAllowedRowsCountForScale(num);
                    int num2 = RowsCountAssumingScale(num);
                    if (num2 <= allowedRowsCountForScale)
                    {
                        break;
                    }
                    num *= 0.95f;
                }
                return num;
            }
        }

        private Vector2 Size
        {
            get
            {
                return SizeAssumingScale(Scale);
            }
        }

        private float SpacingHorizontal
        {
            get
            {
                return SpacingHorizontalAssumingScale(Scale);
            }
        }

        private float SpacingVertical
        {
            get
            {
                return SpacingVerticalAssumingScale(Scale);
            }
        }

        private int ColonistsPerRow
        {
            get
            {
                return ColonistsPerRowAssumingScale(Scale);
            }
        }

        private int ColonistsPerColumn
        {
            get
            {
                return ColonistsPerColumnAssumingScale(Scale);
            }
        }

        private static Vector2 SizeAssumingScale(float scale)
        {
            BaseSize.x = CBKF.BarSettings.BaseSizeFloat;
            BaseSize.y = CBKF.BarSettings.BaseSizeFloat;
            return BaseSize * scale;
        }

        private int RowsCountAssumingScale(float scale)
        {
            return Mathf.CeilToInt(cachedDrawLocs.Count / (float)ColonistsPerRowAssumingScale(scale));
        }
        private int ColumnsCountAssumingScale(float scale)
        {
            return Mathf.CeilToInt(cachedDrawLocs.Count / (float)ColonistsPerColumnAssumingScale(scale));
        }
        private static int ColonistsPerRowAssumingScale(float scale)
        {
            return Mathf.FloorToInt((CBKF.BarSettings.MaxColonistBarWidth + SpacingHorizontalAssumingScale(scale)) / (SizeAssumingScale(scale).x + SpacingHorizontalAssumingScale(scale)));
        }

        private static int ColonistsPerColumnAssumingScale(float scale)
        {
            return Mathf.FloorToInt((CBKF.BarSettings.MaxColonistBarHeight + SpacingVerticalAssumingScale(scale)) / (SizeAssumingScale(scale).y + SpacingVerticalAssumingScale(scale)));
        }

        private static float SpacingHorizontalAssumingScale(float scale)
        {

            return CBKF.BarSettings.BaseSpacingHorizontal * scale;
        }

        private static float SpacingVerticalAssumingScale(float scale)
        {
            return CBKF.BarSettings.BaseSpacingVertical * scale;
        }

        private static int GetAllowedRowsCountForScale(float scale)
        {
            if (scale > 0.58f)
            {
                return 1;
            }
            if (scale > 0.42f)
            {
                return 2;
            }
            return 3;
        }

        private static int GetAllowedColumnsCountForScale(float scale)
        {

            if (scale > 0.7f)
            {
                return 4;
            }
            if (scale > 0.6f)
            {
                return 5;
            }
            if (scale > 0.5f)
            {
                return 6;
            }

            return 7;

        }

        private static int GetAllowedRowsCountForScaleModded(float scale)
        {
            if (scale > 0.67f)
            {
                return 2;
            }
            if (scale > 0.34f)
            {
                return 3;
            }
            return 4;
        }

        private static List<Thing> tmpColonists = new List<Thing>();

        [Detour(typeof(ColonistBar), bindingFlags = (BindingFlags.Instance | BindingFlags.Public))]
        public void ColonistBarOnGUI()
        {
            if (!Find.PlaySettings.showColonistBar)
            {
                return;
            }
            if (CBKF.BarSettings.Reloadsettings || CBKF.BarSettings.Firstload)
            {
                BaseSize.x = CBKF.BarSettings.BaseSizeFloat;
                BaseSize.y = CBKF.BarSettings.BaseSizeFloat;
                PawnTextureSize.x = CBKF.BarSettings.BaseSizeFloat - 2f;
                PawnTextureSize.y = CBKF.BarSettings.BaseSizeFloat * 1.5f;
                float pawnTextureCameraOffsetNew = CBKF.BarSettings.PawnTextureCameraZoom / 1.28205f;
                PawnTextureCameraOffset = new Vector3(CBKF.BarSettings.PawnTextureCameraHorizontalOffset / pawnTextureCameraOffsetNew, 0f, CBKF.BarSettings.PawnTextureCameraVerticalOffset / pawnTextureCameraOffsetNew);
                CBKF.BarSettings.Firstload = false;
                CBKF.BarSettings.Reloadsettings = false;
                if (CBKF.BarSettings.UseGender)
                    ColonistBarTextures.BGTex = ColonistBarTextures.BGTexGrey;
                else
                {
                    ColonistBarTextures.BGTex = ColonistBarTextures.BGTexVanilla;
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                RecacheDrawLocs();
            }
            else
            {
                for (int i = 0; i < cachedDrawLocs.Count; i++)
                {
                    Rect rect = new Rect(cachedDrawLocs[i].x, cachedDrawLocs[i].y, Size.x, Size.y);
                    Pawn colonist = cachedColonists[i];
                    HandleColonistClicks(rect, colonist);
                    if (Event.current.type == EventType.Repaint)
                    {
                        //Widgets.DrawShadowAround(rect);
                        DrawColonist(rect, colonist);
                    }
                }
            }

        }

        // RimWorld.ColonistBar
        [Detour(typeof(ColonistBar), bindingFlags = (BindingFlags.Instance | BindingFlags.Public))]
        public List<Thing> ColonistsInScreenRect(Rect rect)
        {

            tmpColonists.Clear();
            RecacheDrawLocs();
            for (int i = 0; i < cachedDrawLocs.Count; i++)
            {
                if (rect.Overlaps(new Rect(cachedDrawLocs[i].x, cachedDrawLocs[i].y, Size.x, Size.y)))
                {
                    Thing thing;
                    if (cachedColonists[i].Dead)
                    {
                        thing = cachedColonists[i].corpse;
                    }
                    else
                    {
                        thing = cachedColonists[i];
                    }
                    if (thing != null && thing.Spawned)
                    {
                        tmpColonists.Add(thing);
                    }
                }
            }
            return tmpColonists;
        }

        [Detour(typeof(ColonistBar), bindingFlags = (BindingFlags.Instance | BindingFlags.Public))]
        public Thing ColonistAt(Vector2 pos)
        {
            Pawn pawn = null;
            RecacheDrawLocs();
            for (int i = 0; i < cachedDrawLocs.Count; i++)
            {
                Rect rect = new Rect(cachedDrawLocs[i].x, cachedDrawLocs[i].y, Size.x, Size.y);
                if (rect.Contains(pos))
                {
                    pawn = cachedColonists[i];
                }
            }
            Thing thing;
            if (pawn != null && pawn.Dead)
            {
                thing = pawn.corpse;
            }
            else
            {
                thing = pawn;
            }
            if (thing != null && thing.Spawned)
            {
                return thing;
            }
            return null;
        }


        public void RecacheDrawLocs()
        {
            CheckRecacheColonistsRaw();
            Vector2 size = Size;
            int colonistsPerRow = ColonistsPerRow;
            int colonistsPerColumn = ColonistsPerColumn;
            float spacingHorizontal = SpacingHorizontal;
            float spacingVertical = SpacingVertical;
            float cachedDrawLocs_x = 0f + CBKF.BarSettings.MarginLeftHorTop;
            float cachedDrawLocs_y = CBKF.BarSettings.MarginTopHor;
            if (CBKF.BarSettings.UseVerticalAlignment)
            {
                cachedDrawLocs_x = 0f + CBKF.BarSettings.MarginLeftVer;
                if (CBKF.BarSettings.UseRightAlignment)
                    cachedDrawLocs_x = Screen.width - size.x - CBKF.BarSettings.MarginRightVer;
            }
            else if (CBKF.BarSettings.UseBottomAlignment)
            {
                cachedDrawLocs_y = Screen.height - size.y - CBKF.BarSettings.MarginBottomHor - 30f - 12f;
            }
            cachedDrawLocs.Clear();
            if (CBKF.BarSettings.UseVerticalAlignment)
            {
                for (int i = 0; i < cachedColonists.Count; i++)
                {
                    //         Debug.Log("Colonists count: " + i);
                    if (i % colonistsPerColumn == 0)
                    {
                        int maxColInColumn = Mathf.Min(colonistsPerColumn, cachedColonists.Count - i);
                        float num4 = maxColInColumn * size.y + (maxColInColumn - 1) * spacingVertical;
                        cachedDrawLocs_y = (Screen.height - num4) / 2f + CBKF.BarSettings.VerticalOffset;
                        if (i != 0)
                        {
                            if (CBKF.BarSettings.UseRightAlignment)
                            {
                                cachedDrawLocs_x -= size.x + spacingHorizontal;
                            }
                            else
                            {
                                cachedDrawLocs_x += size.x + spacingHorizontal;
                            }
                        }
                        //         Debug.Log("maxColInColumn " + maxColInColumn);
                    }
                    else
                    {
                        cachedDrawLocs_y += size.y + spacingVertical;
                    }
                    cachedDrawLocs.Add(new Vector2(cachedDrawLocs_x, cachedDrawLocs_y));

                    //      Debug.Log("MaxColonistBarHeight:" + BarSettings.MaxColonistBarHeight+ " + SpacingVerticalAssumingScale(1f): "+ SpacingVerticalAssumingScale(1f) + " / (SizeAssumingScale(1f).y: "+ SizeAssumingScale(1f).y + " + SpacingVerticalAssumingScale(1f): "+ SpacingVerticalAssumingScale(1f));
                    //
                    //      Debug.Log("colonistsPerRow " + colonistsPerRow);
                    //      Debug.Log("colonistsPerColumn " + colonistsPerColumn);
                    //      Debug.Log("cachedDrawLocs_x: " + cachedDrawLocs_x);
                    //      Debug.Log("cachedDrawLocs_y: " + cachedDrawLocs_y);
                    //      Debug.Log("cachedColonists: " + i);

                }
            }
            else
            {
                for (int i = 0; i < cachedColonists.Count; i++)
                {
                    if (i % colonistsPerRow == 0)
                    {
                        int maxColInRow = Mathf.Min(colonistsPerRow, cachedColonists.Count - i);
                        float num4 = maxColInRow * size.x + (maxColInRow - 1) * spacingHorizontal;
                        cachedDrawLocs_x = (Screen.width - num4) / 2f + CBKF.BarSettings.HorizontalOffset;
                        if (i != 0)
                        {
                            if (CBKF.BarSettings.UseBottomAlignment)
                            {
                                cachedDrawLocs_y -= size.y + spacingVertical;
                            }
                            else
                            {
                                cachedDrawLocs_y += size.y + spacingVertical;
                            }
                        }
                    }
                    else
                    {
                        cachedDrawLocs_x += size.x + spacingHorizontal;
                    }
                    cachedDrawLocs.Add(new Vector2(cachedDrawLocs_x, cachedDrawLocs_y));
                }
            }
        }

        private void CheckRecacheColonistsRaw()
        {
            if (!colonistsDirty)
            {
                return;
            }
            cachedColonists.Clear();

            cachedColonists.AddRange(Find.MapPawns.FreeColonists);

            List<Thing> list = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsDessicated())
                {
                    Pawn innerPawn = ((Corpse)list[i]).innerPawn;
                    if (innerPawn.IsColonist)
                    {
                        cachedColonists.Add(innerPawn);
                    }
                }
            }
            List<Pawn> allPawnsSpawned = Find.MapPawns.AllPawnsSpawned;
            for (int j = 0; j < allPawnsSpawned.Count; j++)
            {
                Corpse corpse = allPawnsSpawned[j].carrier.CarriedThing as Corpse;
                if (corpse != null && !corpse.IsDessicated() && corpse.innerPawn.IsColonist)
                {
                    cachedColonists.Add(corpse.innerPawn);
                }
            }

            SortCachedColonists();

            pawnLabelsCache.Clear();
            colonistsDirty = false;
        }

        public void SortCachedColonists()
        {
            IOrderedEnumerable<Pawn> orderedEnumerable = null;
            switch (CBKF.BarSettings.SortBy)
            {
                case vanilla:
                    cachedColonists.SortBy(x => x.thingIDNumber);
                    break;

                case byName:
                    orderedEnumerable = cachedColonists.OrderBy(x => x.LabelCap != null).ThenByDescending(x => x.LabelCap);
                    cachedColonists = orderedEnumerable.ToList();
                    break;

                case sexage:
                    orderedEnumerable = cachedColonists.OrderBy(x => x.gender.GetLabel() != null).ThenBy(x => x.gender.GetLabel()).ThenBy(x => x.ageTracker.AgeBiologicalYears);
                    cachedColonists = orderedEnumerable.ToList();
                    break;

                case health:
                    orderedEnumerable = cachedColonists.OrderBy(x => x?.health?.summaryHealth?.SummaryHealthPercent);
                    cachedColonists = orderedEnumerable.ToList();
                    break;

                case mood:
                    orderedEnumerable = cachedColonists.OrderByDescending(x => x?.needs?.mood?.CurLevelPercentage);
                    cachedColonists = orderedEnumerable.ToList();
                    break;

                case weapons:
                    //orderedEnumerable = cachedColonists
                    //    .OrderByDescending(x => x.equipment.Primary != null && x.equipment.Primary.def.IsMeleeWeapon)
                    //    .ThenByDescending(x => x.equipment.Primary != null && x.equipment.Primary.def.IsRangedWeapon);

                    orderedEnumerable = cachedColonists
                        .OrderByDescending(a => a.equipment.Primary != null && a.equipment.Primary.def.IsMeleeWeapon)
                        //.GetSkill(SkillDefOf.Melee).level)
                        .ThenByDescending(c => c.equipment.Primary != null && c.equipment.Primary.def.IsRangedWeapon)
                        .ThenByDescending(b => b.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting));
                    //                    .ThenByDescending(d => d.skills.GetSkill(SkillDefOf.Shooting).level);

                    cachedColonists = orderedEnumerable.ToList();
                    break;

                case medic:

                    orderedEnumerable = cachedColonists
                        .OrderByDescending(b => b.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor));

                    cachedColonists = orderedEnumerable.ToList();
                    break;

                default:
                    cachedColonists.SortBy(x => x.thingIDNumber);
                    break;
            }

        }

        private void DrawColonist(Rect rect, Pawn colonist)
        {
            float colonistRectAlpha = GetColonistRectAlpha(rect);
            bool colonistAlive = !colonist.Dead ? Find.Selector.SelectedObjects.Contains(colonist) : Find.Selector.SelectedObjects.Contains(colonist.corpse);
            Color color = new Color(1f, 1f, 1f, colonistRectAlpha);
            GUI.color = color;

            Color BGColor = new Color();

            Need_Mood mood = (!colonist.Dead) ? colonist.needs.mood : null;
            MentalBreaker mb = (!colonist.Dead) ? colonist.mindState.mentalBreaker : null;

            if (CBKF.BarSettings.UseMoodColors)
            {

                Rect moodBorderRect = rect.ContractedBy(rect.width - 1f);
                moodBorderRect.width *= CBKF.BarSettings.moodRectScale;
                moodBorderRect.height *= CBKF.BarSettings.moodRectScale;
                if (CBKF.BarSettings.moodRectScale < 1f)
                {
                    moodBorderRect.x -= rect.width / 8 * Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale);
                    moodBorderRect.y = rect.yMin + 1f - moodBorderRect.height + 8 * Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale);
                }



                if (mood != null && mb != null)
                {
                    if (mood.CurLevelPercentage <= mb.BreakThresholdExtreme)
                    {
                        GUI.DrawTexture(moodBorderRect, ColonistBarTextures.MoodExtremeCrossedTex);
                    }
                    else if (mood.CurLevelPercentage <= mb.BreakThresholdMajor)
                    {
                        GUI.DrawTexture(moodBorderRect, ColonistBarTextures.MoodMajorCrossedTex);
                    }
                    else if (mood.CurLevelPercentage <= mb.BreakThresholdMinor)
                    {
                        GUI.DrawTexture(moodBorderRect, ColonistBarTextures.MoodMinorCrossedTex);
                    }
                }
            }
            if (CBKF.BarSettings.UseGender)
            {
                if (colonist.gender == Gender.Male)
                {
                    BGColor = CBKF.BarSettings.MaleColor;
                }
                if (colonist.gender == Gender.Female)
                {
                    BGColor = CBKF.BarSettings.FemaleColor;
                }
            }
            if (colonist.Dead)
                BGColor = BGColor * Color.gray;

            // else if (colonist.needs.mood.CurLevel < colonist.mindState.mentalBreaker.BreakThresholdMinor)
            // {
            //     BGColor = Color.Lerp(Color.red, BGColor, colonist.needs.mood.CurLevel / colonist.mindState.mentalBreaker.BreakThresholdMinor);
            // }
            BGColor.a = colonistRectAlpha;
            if (CBKF.BarSettings.UseGender)
            {
                GUI.color = BGColor;
            }


            // adding color overlay

            GUI.DrawTexture(rect, ColonistBarTextures.BGTex);
            GUI.color = color;
            if (CBKF.BarSettings.UseMoodColors)
            {
                // draw mood thingie

                Rect moodRect = rect.ContractedBy(rect.width - 1f);
                moodRect.width *= CBKF.BarSettings.moodRectScale;
                moodRect.height *= CBKF.BarSettings.moodRectScale;
                if (CBKF.BarSettings.moodRectScale < 1f)
                {
                    moodRect.x -= rect.width / 8 * Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale);
                    moodRect.y = rect.yMin + 1f - moodRect.height + 8 * Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale);
                }


                if (mood != null && mb != null)
                {
                    //                    GUI.DrawTexture(moodRect, ColonistBarTextures.MoodBGTex);
                    if (mood.CurLevelPercentage > mb.BreakThresholdMinor)
                    {
                        GUI.color = new Color(1, 1, 1, Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale) * color.a);
                        GUI.DrawTexture(moodRect, ColonistBarTextures.MoodGoodTex);
                        GUI.color = color;
                        GUI.DrawTexture(moodRect.TopPart(Mathf.InverseLerp(1f, mb.BreakThresholdMinor, mood.CurLevelPercentage)), ColonistBarTextures.MoodNeutral);
                    }
                    else if (mood.CurLevelPercentage > mb.BreakThresholdMajor)
                    {
                        GUI.color = new Color(1, 1, 1, Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale) * color.a + 0.2f);
                        GUI.DrawTexture(moodRect, ColonistBarTextures.MoodNeutral);
                        GUI.color = color;
                        GUI.DrawTexture(moodRect.TopPart(Mathf.InverseLerp(mb.BreakThresholdMinor, mb.BreakThresholdMajor, mood.CurLevelPercentage)), ColonistBarTextures.MoodMinorCrossedTex);
                    }
                    else if (mood.CurLevelPercentage > mb.BreakThresholdExtreme)
                    {
                        GUI.color = new Color(1, 1, 1, Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale) * color.a + 0.3f);
                        GUI.DrawTexture(moodRect, ColonistBarTextures.MoodMinorCrossedTex);
                        GUI.color = color;
                        GUI.DrawTexture(moodRect.TopPart(Mathf.InverseLerp(mb.BreakThresholdMajor, mb.BreakThresholdExtreme, mood.CurLevelPercentage)), ColonistBarTextures.MoodMajorCrossedTex);
                    }
                    else
                    {
                        GUI.color = new Color(1, 1, 1, Mathf.InverseLerp(1f, 0.33f, CBKF.BarSettings.moodRectScale) * color.a + 0.4f);
                        GUI.DrawTexture(moodRect, ColonistBarTextures.MoodMajorCrossedTex);
                        GUI.color = color;
                        GUI.DrawTexture(moodRect.TopPart(Mathf.InverseLerp(mb.BreakThresholdExtreme, 0f, mood.CurLevelPercentage)), ColonistBarTextures.MoodExtremeCrossedTex);
                    }

                    DrawMentalThresholdExt(moodRect, mb.BreakThresholdExtreme);
                    DrawMentalThresholdMaj(moodRect, mb.BreakThresholdMajor);
                    DrawMentalThresholdMin(moodRect, mb.BreakThresholdMinor);

                    GUI.color = Color.black;
                    GUI.DrawTexture(new Rect(moodRect.x, moodRect.yMin + moodRect.height * mood.CurInstantLevelPercentage, moodRect.width, 1), ColonistBarTextures.MoodTargetTex);

                    GUI.color = Color.cyan;
                    GUI.DrawTexture(new Rect(moodRect.xMax + 1, moodRect.yMin + moodRect.height * mood.CurInstantLevelPercentage - 1, 2, 3), ColonistBarTextures.MoodTargetTex);
                    GUI.color = color;
                }

            }
            if (colonistAlive)
            {
                DrawSelectionOverlayOnGUI(colonist, rect.ContractedBy(-2f * Scale));
            }

            GUI.DrawTexture(GetPawnTextureRect(rect.x, rect.y), PortraitsCache.Get(colonist, PawnTextureSize, PawnTextureCameraOffset, CBKF.BarSettings.PawnTextureCameraZoom));

            if (CBKF.BarSettings.UseWeaponIcons)
            {
                DrawWeapon(rect, colonist);
                if (!CBKF.BarSettings.UseCustomPawnTextureCameraHorizontalOffset)
                {
                    CBKF.BarSettings.PawnTextureCameraHorizontalOffset = 0.3f;
                }
            }

            GUI.color = new Color(1f, 1f, 1f, colonistRectAlpha * 0.8f);
            DrawIcons(rect, colonist);
            GUI.color = color;
            if (colonist.Dead)
            {
                GUI.DrawTexture(rect, ColonistBarTextures.DeadColonistTex);
            }
            float num = 4f * Scale;
            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num);
            GenWorldUI.DrawPawnLabel(colonist, pos, colonistRectAlpha, rect.width + SpacingHorizontal - 2f, pawnLabelsCache);
            GUI.color = Color.white;
        }
        private static readonly Color _highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private void DrawWeapon(Rect rect, Pawn colonist)
        {
            float colonistRectAlpha = GetColonistRectAlpha(rect);
            Color color = new Color(1f, 1f, 1f, colonistRectAlpha);
            GUI.color = color;
            foreach (ThingWithComps thing in colonist.equipment.AllEquipment)
            {
                var rect2 = rect.ContractedBy(rect.width / 3);

                rect2.x += rect.width / 3 - (rect.width / 8);
                rect2.y += rect.height / 3 - (rect.height / 8);

                if (Mouse.IsOver(rect2))
                {
                    GUI.color = _highlightColor;
                    GUI.DrawTexture(rect2, TexUI.HighlightTex);
                }

                GUI.color = Color.white;
                Texture2D resolvedIcon;
                if (!thing.def.uiIconPath.NullOrEmpty())
                {
                    resolvedIcon = thing.def.uiIcon;
                }
                else
                {
                    resolvedIcon = thing.Graphic.ExtractInnerGraphicFor(thing).MatSingle.mainTexture as Texture2D;
                }
                // color labe by thing

                var iconcolor = new Color(0.8f, 0.8f, 0.8f, colonistRectAlpha * 0.75f);
                if (thing.def.IsMeleeWeapon)
                {
                    GUI.color = new Color(0.7f, 0.1f, 0.1f, colonistRectAlpha);
                }
                if (thing.def.IsRangedWeapon)
                {
                    GUI.color = new Color(0.1f, 0.7f, 0.1f, colonistRectAlpha);
                }
                Widgets.DrawBoxSolid(rect2, iconcolor);
                Widgets.DrawBox(rect2);
                GUI.color = Color.white;
                var rect3 = rect2.ContractedBy(rect2.width / 8);

                Widgets.DrawTextureRotated(rect3, resolvedIcon, 0);
            }
        }



        internal static void DrawMentalThresholdExt(Rect moodRect, float threshold)
        {
            GUI.DrawTexture(new Rect(moodRect.x, moodRect.yMin + moodRect.height * threshold, moodRect.width, 1), ColonistBarTextures.MoodExtremeCrossedTex);
        }
        internal static void DrawMentalThresholdMaj(Rect moodRect, float threshold)
        {
            GUI.DrawTexture(new Rect(moodRect.x, moodRect.yMin + moodRect.height * threshold, moodRect.width, 1), ColonistBarTextures.MoodMajorCrossedTex);
        }
        internal static void DrawMentalThresholdMin(Rect moodRect, float threshold)
        {
            GUI.DrawTexture(new Rect(moodRect.x, moodRect.yMin + moodRect.height * threshold, moodRect.width, 1), ColonistBarTextures.MoodMinorCrossedTex);
        }
        private float GetColonistRectAlpha(Rect rect)
        {
            float t;
            if (Messages.CollidesWithAnyMessage(rect, out t))
            {
                return Mathf.Lerp(1f, 0.2f, t);
            }
            return 1f;
        }

        private Rect GetPawnTextureRect(float x, float y)
        {
            Vector2 vector = PawnTextureSize * Scale;
            return new Rect(x + 1f, y - (vector.y - Size.y) - 1f, vector.x, vector.y);
        }

        private void DrawIcons(Rect rect, Pawn colonist)
        {
            if (colonist.Dead)
            {
                return;
            }
            float num = CBKF.BarSettings.BaseIconSize * Scale;
            Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);
            bool flag = false;
            if (colonist.CurJob != null)
            {
                JobDef def = colonist.CurJob.def;
                if (def == JobDefOf.AttackMelee || def == JobDefOf.AttackStatic)
                {
                    flag = true;
                }
                else if (def == JobDefOf.WaitCombat)
                {
                    Stance_Busy stance_Busy = colonist.stances.curStance as Stance_Busy;
                    if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
                    {
                        flag = true;
                    }
                }
            }
            if (colonist.InAggroMentalState)
            {
                DrawIcon(ColonistBarTextures.Icon_MentalStateAggro, ref vector, colonist.MentalStateDef.LabelCap);
            }
            else if (colonist.InMentalState)
            {
                DrawIcon(ColonistBarTextures.Icon_MentalStateNonAggro, ref vector, colonist.MentalStateDef.LabelCap);
            }
            else if (colonist.InBed() && colonist.CurrentBed().Medical)
            {
                DrawIcon(ColonistBarTextures.Icon_MedicalRest, ref vector, "ActivityIconMedicalRest".Translate());
            }
            else if (colonist.CurJob != null && colonist.jobs.curDriver.asleep)
            {
                DrawIcon(ColonistBarTextures.Icon_Sleeping, ref vector, "ActivityIconSleeping".Translate());
            }
            else if (colonist.CurJob != null && colonist.CurJob.def == JobDefOf.FleeAndCower)
            {
                DrawIcon(ColonistBarTextures.Icon_Fleeing, ref vector, "ActivityIconFleeing".Translate());
            }
            else if (flag)
            {
                DrawIcon(ColonistBarTextures.Icon_Attacking, ref vector, "ActivityIconAttacking".Translate());
            }
            else if (colonist.mindState.IsIdle && GenDate.DaysPassed >= 1)
            {
                DrawIcon(ColonistBarTextures.Icon_Idle, ref vector, "ActivityIconIdle".Translate());
            }
            if (colonist.IsBurning())
            {
                DrawIcon(ColonistBarTextures.Icon_Burning, ref vector, "ActivityIconBurning".Translate());
            }
            // custom 

            //       if (BarSettings.useExtraIcons)
            //       {
            //           if (colonist.needs.mood.CurLevel < colonist.mindState.mentalBreaker.BreakThresholdMinor)
            //           {
            //               GUI.color = Color.Lerp(Color.red, Color.grey, colonist.needs.mood.CurLevel / colonist.mindState.mentalBreaker.BreakThresholdMinor);
            //               Icon_Sad = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar_KF/Sad", true);
            //               DrawIcon(Icon_Sad, ref vector, "Sad".Translate());
            //               GUI.color = Color.white;
            //           } 
            //       }

        }

        private void DrawIcon(Texture2D icon, ref Vector2 pos, string tooltip)
        {
            float num = CBKF.BarSettings.BaseIconSize * Scale;
            Rect rect = new Rect(pos.x, pos.y, num, num);
            GUI.DrawTexture(rect, icon);
            TooltipHandler.TipRegion(rect, tooltip);
            pos.x += num;
        }



        private void HandleColonistClicks(Rect rect, Pawn colonist)
        {
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseDown)
            {
                if (clickedColonist == colonist && Time.time - clickedAt < CBKF.BarSettings.DoubleClickTime)
                {
                    // use event so it doesn't bubble through
                    Event.current.Use();
                    JumpToTargetUtility.TryJump(colonist);
                    clickedColonist = null;
                }
                else
                {
                    clickedColonist = colonist;
                    clickedAt = Time.time;
                }
            }
            if (Mouse.IsOver(rect) && Event.current.button == 1)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();

                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.Vanilla".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = vanilla;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));
                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.ByName".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = byName;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));

                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.SexAge".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = sexage;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));

                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.Mood".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = mood;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));
                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.Health".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = health;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));
                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.Medic".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = medic;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));
                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.Weapons".Translate(), delegate
                    {
                        CBKF.BarSettings.SortBy = weapons;
                        ((UIRootMap)Find.UIRoot).colonistBar.MarkColonistsListDirty();
                    }));

                    floatOptionList.Add(new FloatMenuOption("ColonistBarKF.BarSettings.barSettings".Translate(), delegate
                    {
                        Find.WindowStack.Add(new ColonistBarKF_Settings());
                    }));
                    FloatMenu window = new FloatMenu(floatOptionList, "ColonistBarKF.BarSettings.SortingOptions".Translate());
                    Find.WindowStack.Add(window);

                    // use event so it doesn't bubble through
                    Event.current.Use();
                }
            }

        }

        private void DrawSelectionOverlayOnGUI(Pawn colonist, Rect rect)
        {
            Thing thing = colonist;
            if (colonist.Dead)
            {
                thing = colonist.corpse;
            }
            float num = 0.4f * Scale;
            Vector2 textureSize = new Vector2(ColonistBarTextures.SelectedTex.width * num, ColonistBarTextures.SelectedTex.height * num);
            Vector3[] array = SelectionDrawer.SelectionBracketPartsPos(thing, rect.center, rect.size, textureSize, CBKF.BarSettings.BaseIconSize * Scale);
            int num2 = 90;
            for (int i = 0; i < 4; i++)
            {
                Widgets.DrawTextureRotated(new Vector2(array[i].x, array[i].z), ColonistBarTextures.SelectedTex, num2, num);
                num2 += 90;
            }
        }

    }

}
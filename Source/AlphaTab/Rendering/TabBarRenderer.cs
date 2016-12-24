﻿/*
 * This file is part of alphaTab.
 * Copyright (c) 2014, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */
using AlphaTab.Model;
using AlphaTab.Platform;
using AlphaTab.Rendering.Glyphs;
using AlphaTab.Rendering.Utils;

namespace AlphaTab.Rendering
{
    /// <summary>
    /// This BarRenderer renders a bar using guitar tablature notation
    /// </summary>
    public class TabBarRenderer : BarRendererBase
    {
        public const float LineSpacing = 10;

        public TabBarRenderer(Bar bar)
            : base(bar)
        {
        }

        public float LineOffset
        {
            get
            {
                return ((LineSpacing + 1) * Scale);
            }
        }

        public override float GetNoteX(Note note, bool onEnd = true)
        {
            var beat = (TabBeatGlyph)GetOnNotesGlyphForBeat(note.Beat);
            if (beat != null)
            {
                return beat.Container.X + beat.Container.VoiceContainer.X + beat.X + beat.NoteNumbers.GetNoteX(note, onEnd);
            }
            return 0;
        }

        public override float GetNoteY(Note note)
        {
            var beat = (TabBeatGlyph)GetOnNotesGlyphForBeat(note.Beat);
            if (beat != null)
            {
                return beat.NoteNumbers.GetNoteY(note);
            }
            return 0;
        }

        public override void DoLayout()
        {
            var res = Resources;
            var numberOverflow = (res.TablatureFont.Size / 2) + (res.TablatureFont.Size * 0.2f);
            TopPadding = numberOverflow;
            BottomPadding = numberOverflow;

            base.DoLayout();

            Height = LineOffset * (Bar.Staff.Track.Tuning.Length - 1) + (numberOverflow * 2);
            if (Index == 0)
            {
                Staff.RegisterStaveTop(TopOverflow);
                Staff.RegisterStaveBottom(Height - BottomPadding);
            }
        }

        protected override void CreatePreBeatGlyphs()
        {
            if (Bar.MasterBar.IsRepeatStart)
            {
                AddPreBeatGlyph(new RepeatOpenGlyph(0, 0, 1.5f, 3));
            }

            // Clef
            if (IsFirstOfLine)
            {
                var center = (Bar.Staff.Track.Tuning.Length + 1) / 2f;
                AddPreBeatGlyph(new TabClefGlyph(5 * Scale, GetTabY(center)));
            }

            AddPreBeatGlyph(new BarNumberGlyph(0, GetTabY(-1, -3), Bar.Index + 1, !Staff.IsFirstInAccolade));

            if (Bar.IsEmpty)
            {
                AddPreBeatGlyph(new SpacingGlyph(0, 0, 30 * Scale));
            }
        }

        protected override void CreateBeatGlyphs()
        {
            for (int v = 0; v < Bar.Voices.Count; v++)
            {
                CreateVoiceGlyphs(Bar.Voices[v]);
            }
        }

        private void CreateVoiceGlyphs(Voice v)
        {
            for (int i = 0, j = v.Beats.Count; i < j; i++)
            {
                var b = v.Beats[i];
                var container = new TabBeatContainerGlyph(b, GetOrCreateVoiceContainer(v));
                container.PreNotes = new TabBeatPreNotesGlyph();
                container.OnNotes = new TabBeatGlyph();
                AddBeatGlyph(container);
            }
        }

        protected override void CreatePostBeatGlyphs()
        {
            base.CreatePostBeatGlyphs();
            if (Bar.MasterBar.IsRepeatEnd)
            {
                AddPostBeatGlyph(new RepeatCloseGlyph(X, 0));
                if (Bar.MasterBar.RepeatCount > 2)
                {
                    var line = IsLast || IsLastOfLine ? -1 : -4;
                    AddPostBeatGlyph(new RepeatCountGlyph(0, GetTabY(line, -3), Bar.MasterBar.RepeatCount));
                }
            }
            else if (Bar.MasterBar.IsDoubleBar)
            {
                AddPostBeatGlyph(new BarSeperatorGlyph(0, 0));
                AddPostBeatGlyph(new SpacingGlyph(0, 0, 3 * Scale));
                AddPostBeatGlyph(new BarSeperatorGlyph(0, 0));
            }
            else if (Bar.NextBar == null || !Bar.NextBar.MasterBar.IsRepeatStart)
            {
                AddPostBeatGlyph(new BarSeperatorGlyph(0, 0, IsLast));
            }
        }

        /// <summary>
        /// Gets the relative y position of the given steps relative to first line.
        /// </summary>
        /// <param name="line">the amount of steps while 2 steps are one line</param>
        /// <param name="correction"></param>
        /// <returns></returns>
        public float GetTabY(float line, float correction = 0)
        {
            return (LineOffset * line) + (correction * Scale);
        }

        protected override void PaintBackground(float cx, float cy, ICanvas canvas)
        {
            base.PaintBackground(cx, cy, canvas);

            var res = Resources;

            //
            // draw string lines
            //
            canvas.Color = res.StaveLineColor;
            var lineY = cy + Y + TopPadding;

            for (int i = 0, j = Bar.Staff.Track.Tuning.Length; i < j; i++)
            {
                if (i > 0) lineY += LineOffset;
                canvas.BeginPath();
                canvas.MoveTo(cx + X, (int)lineY);
                canvas.LineTo(cx + X + Width, (int)lineY);
                canvas.Stroke();
            }

            canvas.Color = res.MainGlyphColor;

            // Info guides for debugging

            //DrawInfoGuide(canvas, cx, cy, 0, new Color(255, 0, 0)); // top
            //DrawInfoGuide(canvas, cx, cy, stave.StaveTop, new Color(0, 255, 0)); // stavetop
            //DrawInfoGuide(canvas, cx, cy, stave.StaveBottom, new Color(0,255,0)); // stavebottom
            //DrawInfoGuide(canvas, cx, cy, Height, new Color(255, 0, 0)); // bottom
        }

        //private void DrawInfoGuide(ICanvas canvas, int cx, int cy, int y, Color c)
        //{
        //    canvas.Color = c;
        //    canvas.BeginPath();
        //    canvas.MoveTo(cx + X, cy + Y + y);
        //    canvas.LineTo(cx + X + Width, cy + Y + y);
        //    canvas.Stroke();
        //}
    }
}

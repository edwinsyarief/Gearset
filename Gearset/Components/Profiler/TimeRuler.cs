﻿using System;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    public class TimeRuler : UIView
    {    
        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        /// <summary>
        /// Height(in pixels) of level.
        /// </summary>
        const int BarHeight = 8;

        /// <summary>
        /// Gets/Set log display or no.
        /// </summary>
        public bool ShowLog { get; set; }

        /// <summary>
        /// Gets/Sets target sample frames.
        /// </summary>
        public int TargetSampleFrames { get; set; }

        /// <summary>Maximum display frames.</summary>
        const int MaxSampleFrames = 4;

        /// <summary>
        /// Padding(in pixels) of level.
        /// </summary>
        const int BarPadding = 2;

        /// <summary>
        /// Delay frame count for auto display frame adjustment.
        /// </summary>
        const int AutoAdjustDelay = 30;

        // Display frame adjust counter.
        int _frameAdjust;

        // Current display frame count.
        int _sampleFrames;

        internal TimeRuler(int sampleFrames, Vector2 position, Vector2 size) : base(position, size)
        {
            Visible = true;
            _sampleFrames = sampleFrames;
        }

        internal void Draw(Profiler.FrameLog frameLog)
        {
            if (!Visible)
                return;

            var width = Width;

            // Adjust size and position based of number of levels we should draw.
            var height = 0;
            float maxTime = 0;
            for (var levelId = 0; levelId < frameLog.Levels.Length; levelId++)
            {
                var level = frameLog.Levels[levelId];

                if (level.MarkCount <= 0 || DrawLevel(levelId) == false)
                    continue;

                height += BarHeight + BarPadding * 2;
                maxTime = Math.Max(maxTime, level.Markers[level.MarkCount - 1].EndTime);
            }

            Size = new Vector2(width, height);

            // Auto display frame adjustment. If the entire process of frame doesn't finish in less than 16.6ms
            // then it will adjust display frame duration as 33.3ms.
            const float frameSpan = 1.0f / 60.0f * 1000f;
            var sampleSpan = _sampleFrames * frameSpan;

            if (maxTime > sampleSpan)
                _frameAdjust = Math.Max(0, _frameAdjust) + 1;
            else
                _frameAdjust = Math.Min(0, _frameAdjust) - 1;

            if (Math.Abs(_frameAdjust) > AutoAdjustDelay)
            {
                _sampleFrames = Math.Min(MaxSampleFrames, _sampleFrames);
                _sampleFrames = Math.Max(TargetSampleFrames, (int)(maxTime / frameSpan) + 1);

                _frameAdjust = 0;
            }

            // Compute factor that converts from ms to pixel.
            var msToPs = width / sampleSpan;

            var position = Position;
            GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + Size, new Color(56, 56, 56, 150), new Color(16, 16, 16, 127));

            // Draw markers for each levels.
            var s = new Vector2(0, BarHeight);
            var y = position.Y;
            for (var levelId = 0; levelId < frameLog.Levels.Length; levelId++)
            {
                if (DrawLevel(levelId) == false)
                    continue;

                var level = frameLog.Levels[levelId];

                position.Y = y + BarPadding;
                if (level.MarkCount > 0)
                {
                    for (var j = 0; j < level.MarkCount; ++j)
                    {
                        var bt = level.Markers[j].BeginTime;
                        var et = level.Markers[j].EndTime;
                        var sx = (int) (Position.X + bt*msToPs);
                        var ex = (int) (Position.X + et*msToPs);
                        position.X = sx;
                        s.X = Math.Max(ex - sx, 1);
                        GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s,
                            level.Markers[j].Color, level.Markers[j].Color);
                    }
                }

                y += BarHeight + BarPadding;
            }

            // Draw grid lines (each one represents 1 ms of time)
            position = Position;
            s = new Vector2(1, height);
            for (var t = 1.0f; t < sampleSpan; t += 1.0f)
            {
                position.X = (int)(Position.X + t * msToPs);
                GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s, Color.White, Color.White);
            }

            // Draw frame grid.
            for (var i = 0; i <= _sampleFrames; ++i)
            {
                position.X = (int)(Position.X + frameSpan * i * msToPs);
                GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s, Color.Green, Color.Green);
            }
        }
    }
}

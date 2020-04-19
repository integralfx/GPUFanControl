﻿using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GPUFanControl.Models
{
    public class FanCurve : Fan
    {
        private readonly GPU gpu;
        private bool enabled = false;
        public delegate void OnEnabledChanged();
        private readonly OnEnabledChanged onEnabledChanged;

        public FanCurve(ISensor fanSensor, GPU gpu, OnEnabledChanged onEnabledChanged) : 
            base(fanSensor)
        {
            this.gpu = gpu;
            this.onEnabledChanged = onEnabledChanged;

            var points = new List<FanCurvePoint>
            {
                new FanCurvePoint{ Temperature = 40, Percent = 50 },
                new FanCurvePoint{ Temperature = 50, Percent = 75 },
                new FanCurvePoint{ Temperature = 60, Percent = 100 }
            };
            foreach (var point in points)
            {
                AddPoint(point);
            }
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                var previousValue = enabled;
                SetProperty(ref enabled, value);

                if (!value)
                {
                    SetDefault();
                }

                if (previousValue != value)
                {
                    onEnabledChanged();
                }
            }
        }
        public List<SmartFanCurvePoint> Points { get; } = new List<SmartFanCurvePoint>();

        public void AddPoint(FanCurvePoint point)
        {
            var previous = Points.Count() > 0 ? Points.Last() : null;
            var current = new SmartFanCurvePoint
            {
                Temperature = point.Temperature,
                Percent = point.Percent,
                PreviousPoint = previous
            };
            Points.Add(current);
            if (previous != null)
            {
                previous.NextPoint = current;
            }
        }

        public override void Update()
        {
            base.Update();

            if (!enabled)
            {
                return;
            }

            var temperature = gpu.Temperature;
            if (temperature < Points.First().Temperature)
            {
                Percent = Points.First().Percent;
            }
            else if (temperature > Points.Last().Temperature)
            {
                Percent = Points.Last().Percent;
            }
            else
            {
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    var current = Points[i];
                    var next = Points[i + 1];
                    if (temperature >= current.Temperature && gpu.Temperature <= next.Temperature)
                    {
                        var scale = 1.0 * (next.Percent - current.Percent) / (next.Temperature - current.Temperature);
                        Percent = (int)Math.Round((temperature - current.Temperature) * scale + current.Percent);
                        break;
                    }
                }
            }
        }
    }
}

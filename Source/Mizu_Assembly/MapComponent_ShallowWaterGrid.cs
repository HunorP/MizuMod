﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;

namespace MizuMod
{
    public class MapComponent_ShallowWaterGrid : MapComponent_WaterGrid
    {
        private const float BaseRegenRate = 5000.0f;

        public MapComponent_ShallowWaterGrid(Map map) : base(map)
        {

        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            base.RegenPool(BaseRegenRate);
        }
    }
}
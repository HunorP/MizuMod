﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;

namespace MizuMod
{
    public class JobDriver_DrawFromWaterNet : JobDriver_DrawWater
    {
        private Building_WaterNetWorkTable workTable;
        private WaterNet waterNet;

        public override bool TryMakePreToilReservations()
        {
            if (!base.TryMakePreToilReservations()) return false;

            this.workTable = this.job.GetTarget(BillGiverInd).Thing as Building_WaterNetWorkTable;
            if (this.workTable == null) return false;

            this.waterNet = workTable.InputWaterNet;
            if (this.waterNet == null) return false;

            return true;
        }

        protected override void SetFailCondition()
        {
        }

        protected override Thing FinishAction()
        {
            // 水道網の水の種類から水アイテムの種類を決定
            var waterThingDef = MizuUtility.GetWaterThingDefFromWaterType(this.workTable.InputWaterNet.WaterType);
            if (waterThingDef == null) return null;

            // 地下水脈から水を減らす
            this.workTable.InputWaterNet.DrawWaterVolume(recipe.needWaterVolume);

            // 水を生成
            var createThing = ThingMaker.MakeThing(waterThingDef);
            if (createThing == null) return null;

            // 個数設定
            createThing.stackCount = recipe.getItemCount;
            return createThing;
        }
    }
}

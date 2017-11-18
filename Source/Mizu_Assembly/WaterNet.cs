﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace MizuMod
{
    public class WaterNet
    {
        private static int nextID = 1;
        public static void ClearNextID()
        {
            nextID = 1;
        }

        public int ID = 0;

        private List<IBuilding_WaterNet> things = new List<IBuilding_WaterNet>();
        public List<IBuilding_WaterNet> Things
        {
            get
            {
                return things;
            }
        }

        private WaterType waterType = WaterType.NoWater;
        public WaterType WaterType
        {
            get
            {
                return this.waterType;
            }
        }
        public float LastOutputWaterFlow { get; private set; }
        public float LastInputWaterFlow { get; private set; }

        public MapComponent_WaterNetManager Manager { get; set; }

        public WaterNet()
        {
            this.ID = nextID;
            this.waterType = WaterType.NoWater;
            nextID++;
        }

        public WaterNet(IBuilding_WaterNet thing) : this()
        {
            this.AddThing(thing);
        }

        public void AddThing(IBuilding_WaterNet thing)
        {
            thing.WaterNet = this;
            things.Add(thing);
        }

        public void RemoveThing(IBuilding_WaterNet thing)
        {
            thing.WaterNet = null;
            things.Remove(thing);
        }

        public void ClearThings()
        {
            foreach (var thing in things)
            {
                thing.WaterNet = null;
            }
            things.Clear();
        }

        // 仮
        public float StoredWaterVolume
        {
            get
            {
                List<IBuilding_WaterNet> tanks = things.FindAll((t) => t.GetComp<CompWaterNetTank>() != null);

                float sumStoredWaterVolume = 0.0f;
                foreach (var tank in tanks)
                {
                    sumStoredWaterVolume += tank.GetComp<CompWaterNetTank>().StoredWaterVolume;
                }

                return sumStoredWaterVolume;
            }
        }

        // 仮
        public void DrawWaterVolume(float amount)
        {
            float totalAmount = amount;

            while (totalAmount > 0.0f)
            {
                List<IBuilding_WaterNet> tanks = things.FindAll((t) =>
                {
                    CompWaterNetTank compTank = t.GetComp<CompWaterNetTank>();
                    return (compTank != null) && (compTank.StoredWaterVolume > 0.0f);
                });

                if (tanks.Count == 0)
                {
                    break;
                }

                float averageAmount = totalAmount / tanks.Count;
                foreach (var tank in tanks)
                {
                    totalAmount -= tank.GetComp<CompWaterNetTank>().DrawWaterVolume(averageAmount);
                }
            }
        }

        public void AddWaterVolume(float amount)
        {
            float totalAmount = amount;

            while (totalAmount > 0.0f)
            {
                List<IBuilding_WaterNet> tanks = things.FindAll((t) =>
                {
                    CompWaterNetTank tankComp = t.GetComp<CompWaterNetTank>();
                    CompWaterNetInput inputComp = t.GetComp<CompWaterNetInput>();
                    return (tankComp != null) && (tankComp.AmountCanAccept > 0.0f) && (inputComp != null) && (inputComp.InputType == CompProperties_WaterNetInput.InputType.WaterNet);
                });

                if (tanks.Count == 0)
                {
                    break;
                }

                float averageWaterFlow = totalAmount / tanks.Count;
                foreach (var tank in tanks)
                {
                    totalAmount -= tank.GetComp<CompWaterNetTank>().AddWaterVolume(averageWaterFlow);
                }
            }
        }

        public void UpdateOutputWaterFlow()
        {
            // 稼働中の出力機能付き建造物を取得
            List<IBuilding_WaterNet> outputters = things.FindAll((t) =>
            {
                CompWaterNetOutput comp = t.GetComp<CompWaterNetOutput>();
                return comp != null && comp.IsActivated;
            });

            // 全出力の合計値を算出
            float outputWaterFlow = 0.0f;
            foreach (var outputter in outputters)
            {
                outputWaterFlow += outputter.GetComp<CompWaterNetOutput>().OutputWaterFlow;
            }
            this.LastOutputWaterFlow = outputWaterFlow;
        }

        public void UpdateInputWaterFlow()
        {
            // 入力値をクリア
            foreach (var t in things.FindAll((t) => t.GetComp<CompWaterNetInput>() != null))
            {
                t.GetComp<CompWaterNetInput>().InputWaterFlow = 0.0f;
            }

            // 水道網から入力する装置の処理

            // 一定の入力が必要な入力装置を取得
            List<IBuilding_WaterNet> inputters_constant = things.FindAll((t) =>
            {
                CompWaterNetInput comp = t.GetComp<CompWaterNetInput>();
                if (comp == null)
                {
                    return false;
                }
                bool isOK = comp.IsActivated;
                isOK &= comp.InputType == CompProperties_WaterNetInput.InputType.WaterNet;
                isOK &= comp.InputWaterFlowType == CompProperties_WaterNetInput.InputWaterFlowType.Constant;
                return isOK;
            });

            // 一定の入力が必要な入力装置の入力から先に割り振る
            float outputWaterFlow = this.LastOutputWaterFlow;
            foreach (var inputter in inputters_constant)
            {
                CompWaterNetInput comp = inputter.GetComp<CompWaterNetInput>();
                if (outputWaterFlow >= comp.MaxInputWaterFlow)
                {
                    comp.InputWaterFlow = comp.MaxInputWaterFlow;
                    outputWaterFlow -= comp.MaxInputWaterFlow;
                }
            }

            // 余った出力を、入力値が任意で良い入力装置に割り振る
            while (outputWaterFlow > 0.0f)
            {
                List<IBuilding_WaterNet> inputters_any = things.FindAll((t) =>
                {
                    CompWaterNetInput comp = t.GetComp<CompWaterNetInput>();
                    if (comp == null)
                    {
                        return false;
                    }
                    bool isOK = comp.IsActivated;
                    isOK &= comp.InputType == CompProperties_WaterNetInput.InputType.WaterNet;
                    isOK &= comp.InputWaterFlowType == CompProperties_WaterNetInput.InputWaterFlowType.Any;
                    isOK &= comp.InputWaterFlow < comp.MaxInputWaterFlow;
                    return isOK;
                });

                if (inputters_any.Count == 0)
                {
                    break;
                }

                float averageOutputWaterFlow = outputWaterFlow / inputters_any.Count;
                foreach (var inputter in inputters_any)
                {
                    CompWaterNetInput comp = inputter.GetComp<CompWaterNetInput>();
                    float actualInput = Mathf.Min(averageOutputWaterFlow, comp.MaxInputWaterFlow);
                    comp.InputWaterFlow = actualInput;
                    outputWaterFlow -= actualInput;
                }
            }

            this.LastInputWaterFlow = this.LastOutputWaterFlow - outputWaterFlow;

            // 雨からの入力

            List<IBuilding_WaterNet> inputters_rain = things.FindAll((t) =>
            {
                CompWaterNetInput comp = t.GetComp<CompWaterNetInput>();
                if (comp == null)
                {
                    return false;
                }
                bool isOK = comp.IsActivated;
                isOK &= comp.InputType == CompProperties_WaterNetInput.InputType.Rain;
                return isOK;
            });

            foreach (var inputter in inputters_rain)
            {
                CompWaterNetInput comp = inputter.GetComp<CompWaterNetInput>();
                comp.InputWaterFlow = comp.MaxInputWaterFlow * inputter.Map.weatherManager.RainRate;
            }
        }

        public void UpdateWaterTankStorage()
        {
            List<IBuilding_WaterNet> notFullTanks = things.FindAll((t) =>
            {
                CompWaterNetTank comp = t.GetComp<CompWaterNetTank>();
                //return comp != null && comp.AmountCanAccept > 0.0f;
                return comp != null;
            });

            foreach (var tank in notFullTanks)
            {
                CompWaterNetTank tankComp = tank.GetComp<CompWaterNetTank>();
                CompWaterNetInput inputComp = tank.GetComp<CompWaterNetInput>();
                CompWaterNetOutput outputComp = tank.GetComp<CompWaterNetOutput>();
                if (tankComp == null)
                {
                    continue;
                }
                float inputWaterFlow = 0.0f;
                if (inputComp != null)
                {
                    inputWaterFlow = inputComp.InputWaterFlow;
                }
                float outputWaterFlow = 0.0f;
                if (outputComp != null)
                {
                    outputWaterFlow = outputComp.OutputWaterFlow;
                }

                float deltaWaterFlow = inputWaterFlow - outputWaterFlow;
                if (deltaWaterFlow > 0.0f)
                {
                    tankComp.AddWaterVolume(deltaWaterFlow / 60000.0f);
                }
                else if (deltaWaterFlow < 0.0f)
                {
                    tankComp.DrawWaterVolume(-deltaWaterFlow / 60000.0f);
                }
            }

            List<IBuilding_WaterNet> waterNetTanks = things.FindAll((t) =>
            {
                CompWaterNetTank tankComp = t.GetComp<CompWaterNetTank>();
                CompWaterNetInput inputComp = t.GetComp<CompWaterNetInput>();
                return tankComp != null && inputComp != null && inputComp.InputType == CompProperties_WaterNetInput.InputType.WaterNet;
            });
            foreach (var tank in waterNetTanks)
            {
                CompWaterNetTank tankComp = tank.GetComp<CompWaterNetTank>();
                if (tankComp.StoredWaterVolume == 0.0f)
                {
                    tankComp.StoredWaterType = WaterType.NoWater;
                }
            }

            List<IBuilding_WaterNet> rainTanks = things.FindAll((t) =>
            {
                CompWaterNetTank tankComp = t.GetComp<CompWaterNetTank>();
                CompWaterNetInput inputComp = t.GetComp<CompWaterNetInput>();
                return tankComp != null && inputComp != null && inputComp.InputType == CompProperties_WaterNetInput.InputType.Rain;
            });
            foreach (var tank in rainTanks)
            {
                CompWaterNetTank tankComp = tank.GetComp<CompWaterNetTank>();
                if (tankComp.StoredWaterVolume == 0.0f)
                {
                    tankComp.StoredWaterType = WaterType.NoWater;
                }
                else
                {
                    tankComp.StoredWaterType = WaterType.RainWater;
                }
            }
        }

        public void UpdateWaterType()
        {
            WaterType curWaterType = WaterType.NoWater;

            foreach (var t in things)
            {
                CompWaterNetOutput comp = t.GetComp<CompWaterNetOutput>();
                if (comp == null)
                {
                    continue;
                }

                if (comp.OutputWaterType != WaterType.NoWater)
                {
                    if (curWaterType == WaterType.NoWater)
                    {
                        curWaterType = comp.OutputWaterType;
                    }
                    else
                    {
                        curWaterType = (WaterType)Math.Min((int)comp.OutputWaterType, (int)curWaterType);
                    }
                }
            }

            List<IBuilding_WaterNet> tanks = things.FindAll((t) => t.GetComp<CompWaterNetTank>() != null);
            if (curWaterType != WaterType.NoWater)
            {
                this.waterType = curWaterType;
                foreach (var tank in tanks)
                {
                    CompWaterNetInput inputComp = tank.GetComp<CompWaterNetInput>();
                    if (inputComp.InputType == CompProperties_WaterNetInput.InputType.WaterNet)
                    {
                        tank.GetComp<CompWaterNetTank>().StoredWaterType = curWaterType;
                    }
                }
            }
            else
            {
                foreach (var tank in tanks)
                {
                    WaterType tankWaterType = tank.GetComp<CompWaterNetTank>().StoredWaterType;
                    if (tankWaterType != WaterType.NoWater)
                    {
                        if (curWaterType == WaterType.NoWater)
                        {
                            curWaterType = tank.GetComp<CompWaterNetTank>().StoredWaterType;
                        }
                        else
                        {
                            curWaterType = (WaterType)Math.Min((int)tank.GetComp<CompWaterNetTank>().StoredWaterType, (int)curWaterType);
                        }
                    }
                    this.waterType = curWaterType;
                }
            }

        }
    }
}
using System;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Marriage;
using UnityEngine;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>Consumes protocol 11063 effect names and presents them on the global Tip layer.</summary>
    internal static class FlowerEffectPresenter
    {
        private const int EffectDurationMs = 3000;
        private const int LayerRetryDelayMs = 100;

        private static int _generation;
        private static int _runnerGeneration = -1;
        private static UIEffectStage.Handle _currentHandle;

        internal static void Install()
        {
            EventDispatcher.Off<string>(GlobalEvent.EVT_CHAT_FLOWER_EFFECT, OnFlowerEffect);
            EventDispatcher.On<string>(GlobalEvent.EVT_CHAT_FLOWER_EFFECT, OnFlowerEffect);
        }

        internal static void Reset()
        {
            ++_generation;
            _runnerGeneration = -1;
            MarriageModel.Instance.ClearFlowerEffects();
            UIEffectStage.Handle handle = _currentHandle;
            _currentHandle = null;
            handle?.Dispose();
        }

        private static void OnFlowerEffect(string _)
        {
            EnsureRunning();
        }

        private static void EnsureRunning()
        {
            int generation = _generation;
            if (_runnerGeneration == generation) return;
            _runnerGeneration = generation;
            _ = RunAsync(generation);
        }

        private static async Task RunAsync(int generation)
        {
            try
            {
                while (generation == _generation)
                {
                    RectTransform parent = ViewManager.GetLayer(UILayer.Tip) as RectTransform;
                    if (parent == null)
                    {
                        await TimeUtil.Delay(LayerRetryDelayMs);
                        continue;
                    }

                    if (!MarriageModel.Instance.TryDequeueFlowerEffect(out string effectName)) break;
                    if (string.IsNullOrEmpty(effectName))
                    {
                        GameLog.Warn("Chat", "skip empty 11063 flower effect name");
                        continue;
                    }

                    UIEffectStage.Handle handle = null;
                    try
                    {
                        handle = await UIEffectStage.AddAsync(effectName, parent);
                        if (generation != _generation)
                        {
                            handle?.Dispose();
                            return;
                        }
                        if (handle == null)
                        {
                            GameLog.Warn("Chat", "11063 flower effect failed to load: {0}", effectName);
                            continue;
                        }

                        _currentHandle = handle;
                        await TimeUtil.Delay(EffectDurationMs);
                    }
                    catch (Exception ex)
                    {
                        GameLog.Error("Chat", "11063 flower effect failed: {0}; {1}", effectName, ex);
                    }
                    finally
                    {
                        if (ReferenceEquals(_currentHandle, handle)) _currentHandle = null;
                        handle?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                GameLog.Error("Chat", "flower effect presenter loop failed: {0}", ex);
            }
            finally
            {
                if (_runnerGeneration == generation) _runnerGeneration = -1;
                if (generation == _generation && MarriageModel.Instance.FlowerEffects.Count > 0)
                {
                    EnsureRunning();
                }
            }
        }
    }
}

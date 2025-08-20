using Godot;
using Threshold.Core.Enums;
using System;
using System.Collections.Generic;

namespace Threshold.Core
{
    /// <summary>
    /// 基于马尔科夫链的天气状态机
    /// </summary>
    public partial class WeatherMachine : Node
    {
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
        public ulong Seed { get; private set; } = 0;
        // 马尔科夫链转移概率表
        private readonly Dictionary<WeatherType, Dictionary<WeatherType, float>> _transitionMatrix =
            new Dictionary<WeatherType, Dictionary<WeatherType, float>>
        {
            {
                WeatherType.Clear, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Clear, 0.6f },
                    { WeatherType.Cloudy, 0.2f },
                    { WeatherType.Rainy, 0.1f },
                    { WeatherType.Windy, 0.05f },
                    { WeatherType.Overcast, 0.05f }
                }
            },
            {
                WeatherType.Cloudy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Clear, 0.2f },
                    { WeatherType.Cloudy, 0.5f },
                    { WeatherType.Rainy, 0.15f },
                    { WeatherType.Overcast, 0.1f },
                    { WeatherType.Windy, 0.05f }
                }
            },
            {
                WeatherType.Rainy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Rainy, 0.5f },
                    { WeatherType.Clear, 0.1f },
                    { WeatherType.Cloudy, 0.2f },
                    { WeatherType.Stormy, 0.1f },
                    { WeatherType.Drizzle, 0.1f }
                }
            },
            {
                WeatherType.Stormy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Stormy, 0.4f },
                    { WeatherType.Rainy, 0.3f },
                    { WeatherType.Thunderstorm, 0.2f },
                    { WeatherType.Clear, 0.1f }
                }
            },
            {
                WeatherType.Overcast, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Overcast, 0.5f },
                    { WeatherType.Cloudy, 0.3f },
                    { WeatherType.Rainy, 0.1f },
                    { WeatherType.Clear, 0.1f }
                }
            },
            {
                WeatherType.Windy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Windy, 0.5f },
                    { WeatherType.Clear, 0.2f },
                    { WeatherType.Cloudy, 0.2f },
                    { WeatherType.Sandstorm, 0.1f }
                }
            },
            {
                WeatherType.Snowy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Snowy, 0.6f },
                    { WeatherType.Blizzard, 0.2f },
                    { WeatherType.Clear, 0.1f },
                    { WeatherType.Cloudy, 0.1f }
                }
            },
            {
                WeatherType.Foggy, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Foggy, 0.5f },
                    { WeatherType.Clear, 0.2f },
                    { WeatherType.Cloudy, 0.2f },
                    { WeatherType.Rainy, 0.1f }
                }
            },
            {
                WeatherType.Thunderstorm, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Thunderstorm, 0.5f },
                    { WeatherType.Stormy, 0.3f },
                    { WeatherType.Rainy, 0.2f }
                }
            },
            {
                WeatherType.Drizzle, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Drizzle, 0.5f },
                    { WeatherType.Rainy, 0.3f },
                    { WeatherType.Cloudy, 0.2f }
                }
            },
            {
                WeatherType.Blizzard, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Blizzard, 0.5f },
                    { WeatherType.Snowy, 0.3f },
                    { WeatherType.Cloudy, 0.2f }
                }
            },
            {
                WeatherType.Sandstorm, new Dictionary<WeatherType, float>
                {
                    { WeatherType.Sandstorm, 0.5f },
                    { WeatherType.Windy, 0.3f },
                    { WeatherType.Clear, 0.2f }
                }
            }
        };

        // 使用Godot的RNG类替代System.Random
        private RandomNumberGenerator _rng = new RandomNumberGenerator();



        // 构造
        public WeatherMachine(ulong seed)
        {
            Seed = seed;
        }
        public override void _Ready()
        {
            // 如果需要种子，可以设置
            _rng.Seed = Seed;
        }

        /// <summary>
        /// 进行一次天气状态转移
        /// </summary>
        public WeatherType Step()
        {
            if (!_transitionMatrix.ContainsKey(CurrentWeather))
            {
                // 如果没有定义转移，回到晴朗
                CurrentWeather = WeatherType.Clear;
                return CurrentWeather;
            }

            var transitions = _transitionMatrix[CurrentWeather];
            float roll = _rng.Randf();
            float cumulative = 0f;

            foreach (var kvp in transitions)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    CurrentWeather = kvp.Key;
                    return CurrentWeather;
                }
            }

            // 如果概率未覆盖到，默认回到晴朗
            CurrentWeather = WeatherType.Clear;
            return CurrentWeather;
        }

        public WeatherType GetWeather()
        {
            return CurrentWeather;
        }
    }
}
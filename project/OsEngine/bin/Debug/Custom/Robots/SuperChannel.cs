using System;
using OsEngine.Entity;
using OsEngine.Indicators;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CustomIndicators.Scripts
{
    public class SuperChannel : Aindicator
    {
        private enum ExtremumType
        {
            High,
            Low,
        }

        private IndicatorDataSeries _xSeries;
        private IndicatorDataSeries _upperChannel;
        private IndicatorDataSeries _lowerChannel;

        private IndicatorParameterInt _paramRatio;
        private IndicatorParameterInt _paramADX;

        private Aindicator _adx;


        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _paramRatio = CreateParameterInt("Период Индикатора", 100);
                _paramADX = CreateParameterInt("Доп.период", 10);

                _xSeries = CreateSeries("Расчет", Color.DarkGreen, IndicatorChartPaintType.Point, false);
                _upperChannel = CreateSeries("Верхний канал", Color.Yellow, IndicatorChartPaintType.Point, true);
                _lowerChannel = CreateSeries("Нижний канал", Color.White, IndicatorChartPaintType.Point, true);

                _adx = IndicatorsFactory.CreateIndicatorByName("ADX", Name + "_adx", false);
                ((IndicatorParameterInt)_adx.Parameters[0]).Bind(_paramADX);
                ProcessIndicator(Name + "ADX", _adx);
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index < 50)
                return;

            var adxLast = _adx.DataSeries[0].Values.Last();

            if (adxLast == 0)
            {
                return;
            }

            var x = Math.Max(Math.Truncate(_paramRatio.ValueInt / adxLast), 1);

            _xSeries.Values[index] = x;

            _upperChannel.Values[index] = GetExtremum(candles, ExtremumType.High, (int)x, index);
            _lowerChannel.Values[index] = GetExtremum(candles, ExtremumType.Low, (int)x, index);
        }

        private decimal GetExtremum(List<Candle> candles, ExtremumType type, int count, int index)
        {
            List<decimal> values = new List<decimal>();

            if (type == ExtremumType.High)
            {
                for (int i = 0; i < count; i++)
                {
                    values.Add(candles[index - i].High);
                }

                return values.Max();
            }

            else
            {
                for (int i = 0; i < count; i++)
                {
                    values.Add(candles[index - i].Low);
                }

                return values.Min();
            }
        }
    }
}

using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using System.Reflection;
using System;
using OsEngine.Charts.CandleChart.Indicators;
using System.Windows.Forms.DataVisualization.Charting;



[Bot("ATRTrailing")]
public class ATRTrailing : BotPanel  // рабочая версия
{

    public ATRTrailing(string name, StartProgram startProgram)
        : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Regime = CreateParameter("Regime", "On", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
        Volume = CreateParameter("Volume", 3, 1.0m, 50, 4);
        Slippage = CreateParameter("Slipage", 10, 0, 20, 1);
        MultAtr = CreateParameter("Mult ATR", 2.0m, 1.0m, 5.0m, 1);
        LengthAtr = CreateParameter("Length ATR", 5, 3, 21, 1);

        _tab.CandleFinishedEvent += Strateg_CandleFinishedEvent;
        _tab.PositionOpeningSuccesEvent += ReloadTrailingPosition;
        _tab.PositionClosingSuccesEvent += PositionClossing;
        ParametrsChangeByUser += Event_ParametrsChangeByUser;

        
    }

    void Event_ParametrsChangeByUser()
    {
      
    }

    /// <summary>
    /// uniq name
    /// взять уникальное имя
    /// </summary>
    public override string GetNameStrategyType()
    {
        return "ATRTrailing";
    }

    /// <summary>
    /// settings GUI
    /// показать окно настроек
    /// </summary>
    public override void ShowIndividualSettingsDialog()
    {

    }

    /// <summary>
    /// trade tab
    /// вкладка для торговли
    /// </summary>
    private BotTabSimple _tab;

    
    //settings настройки публичные

    /// <summary>
    /// slippage
    /// проскальзывание
    /// </summary>
    public StrategyParameterInt Slippage;

    /// <summary>
    /// volume to inter
    /// фиксированный объем для входа
    /// </summary>
    public StrategyParameterDecimal Volume;

    /// <summary>
    /// regime
    /// режим работы
    /// </summary>
    public StrategyParameterString Regime;

    private StrategyParameterInt LengthAtr;
    private StrategyParameterDecimal MultAtr;

    private decimal _lastPrice;
    private decimal _preLastPrice;
    decimal _stopPrice;
   
    List<decimal> _upLine = new List<decimal>();
    List<decimal> _downLine = new List<decimal>();

    // logic логика

    /// <summary>
    /// candle finished event
    /// событие завершения свечи
    /// </summary>
    private void Strateg_CandleFinishedEvent(List<Candle> candles)
    {
        if (Regime.ValueString == "Off")
        {
            return;
        }

      

        _lastPrice = candles[candles.Count - 1].Close;
        _preLastPrice = candles[candles.Count - 2].Close;

        int index = candles.Count - 1;

        TrueRangeReload(candles, index);

        decimal averTRandMult = ((SummTR(_trueRange, index - LengthAtr.ValueInt, index) / LengthAtr.ValueInt) * MultAtr.ValueDecimal);

        _upLine.Add(candles[index].Close + averTRandMult);

        _downLine.Add(candles[index].Close - averTRandMult);

        if(_upLine.Count < 3) { return; }


        List<Position> openPositions = _tab.PositionsOpenAll;


        if (openPositions != null && openPositions.Count != 0)
        {
            LogicClosePosition(candles, openPositions);
        }

        if (Regime.ValueString == "OnlyClosePosition")
        {
            return;
        }
        if (openPositions == null || openPositions.Count == 0)
        {
            LogicOpenPosition(candles, openPositions);
        }
    }


    /// <summary>
    /// logic close pos
    /// логика закрытия позиции
    /// </summary>
    private void LogicClosePosition(List<Candle> candles, List<Position> position)
    {
        List<Position> openPositions = _tab.PositionsOpenAll;

        for (int i = 0; openPositions != null && i < openPositions.Count; i++)
        {
            ReloadTrailingPosition(openPositions[i]);
        }
    }

    /// <summary>
    /// обновление трейлинга, проверка на пробой стопа
    /// </summary>
    private void ReloadTrailingPosition(Position position)
    {
        List<Position> openPositions = _tab.PositionsOpenAll;

        for (int i = 0; openPositions != null && i < openPositions.Count; i++)
        {
            if (openPositions[i].Direction == Side.Buy)
            {
                decimal valueDown = _downLine[_downLine.Count - 1];
               
                if ( _lastPrice > _preLastPrice && valueDown > _stopPrice)
                {
                    _stopPrice = valueDown;
                }

                if (_lastPrice < _stopPrice ) 
                {
                    _tab.CloseAtLimit(position, valueDown, valueDown - Slippage.ValueInt * _tab.Securiti.PriceStep);
                }
                
            }
            else
            {

                decimal valueUp = _upLine[_upLine.Count - 1];

                if (_lastPrice < _preLastPrice && valueUp < _stopPrice)
                {
                    _stopPrice = valueUp;
                }

                if (_lastPrice > _stopPrice)
                    _tab.CloseAtLimit(position, valueUp, valueUp + Slippage.ValueInt * _tab.Securiti.PriceStep);
            }
        }
    }

    private void PositionClossing(Position pos) 
    {
        List<Position> position = _tab.PositionsOpenAll;

        if (pos.Direction == Side.Sell && position.Count == 0)
        {
            if (Regime.ValueString != "OnlyShort")
            {
                // _tab.BuyAtLimit(Volume.ValueDecimal, _lastPrice + Slippage.ValueInt * _tab.Securiti.PriceStep);
                _tab.BuyAtMarket(Volume.ValueDecimal,"revers");

                _stopPrice = _downLine[_downLine.Count - 1];
            }
        }
        if (pos.Direction == Side.Buy && position.Count == 0)
        {
            if (Regime.ValueString != "OnlyLong")
            {
               // _tab.SellAtLimit(Volume.ValueDecimal, _lastPrice - Slippage.ValueInt * _tab.Securiti.PriceStep);
                _tab.SellAtMarket(Volume.ValueDecimal, "revers");

                _stopPrice = _upLine[_upLine.Count - 1];
            }
        }
       
    }

    /// <summary>
    /// open position logic
    /// логика открытия первой позиции
    /// </summary>
    private void LogicOpenPosition(List<Candle> candles, List<Position> position)
    {
        List<Position> openPositions = _tab.PositionsOpenAll;
        if (openPositions == null || openPositions.Count == 0)
        {
            // long
            if (Regime.ValueString != "OnlyShort")
            {
                if (_lastPrice > _upLine[_upLine.Count - 3])
                {
                    _tab.BuyAtLimit(Volume.ValueDecimal, _lastPrice + Slippage.ValueInt * _tab.Securiti.PriceStep);

                    _stopPrice = _downLine[_downLine.Count - 1];

                }
            }

            // Short
            if (Regime.ValueString != "OnlyLong")
            {
                if (_lastPrice < _downLine[_downLine.Count - 3])
                {
                    _tab.SellAtLimit(Volume.ValueDecimal, _lastPrice - Slippage.ValueInt * _tab.Securiti.PriceStep);

                    _stopPrice = _upLine[_upLine.Count - 1];
                }
            }
            return;
        }
    }

    private List<decimal> _trueRange = new List<decimal>();

    private void TrueRangeReload(List<Candle> candles, int index)
    {
        if (index == 0)
        {
            _trueRange = new List<decimal>();
            _trueRange.Add(0);
            return;
        }

        while (_trueRange.Count - 1 < index)
        {
            _trueRange.Add(0);
        }

        decimal hiToLow = Math.Abs(candles[index].High - candles[index].Low);
        decimal closeToHigh = Math.Abs(candles[index - 1].Close - candles[index].High);
        decimal closeToLow = Math.Abs(candles[index - 1].Close - candles[index].Low);

        _trueRange[index] = Math.Max(Math.Max(hiToLow, closeToHigh), closeToLow);
    }

    public decimal SummTR(List<decimal> trueRange, int startIndex, int endIndex)
    {
        decimal result = 0;

        if (endIndex < startIndex)
        {
            int i = endIndex;
            endIndex = startIndex;
            startIndex = i;
        }

        if (startIndex < 0)
        {
            startIndex = 0;
        }


        for (int i = startIndex + 1; i < endIndex + 1; i++)
        {
            result += _trueRange[i];
        }

        return result;
    }

}
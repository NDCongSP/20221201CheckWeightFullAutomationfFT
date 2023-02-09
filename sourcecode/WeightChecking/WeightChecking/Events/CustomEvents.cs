using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class CustomEvents
    {
        #region refresh
        private bool _refreshStatus = false;

        public bool RefreshStatus
        {
            get => _refreshStatus;
            set
            {
                _refreshStatus = value;
                if (value)
                {
                    OnRefreshAction();
                }
            }
        }

        private event EventHandler _eventHandlerRefreshMasterData;
        public event EventHandler EventHandlerRefreshMasterData
        {
            add
            {
                _eventHandlerRefreshMasterData += value;
            }
            remove
            {
                _eventHandlerRefreshMasterData -= value;
            }
        }

        void OnRefreshAction()
        {
            _eventHandlerRefreshMasterData?.Invoke(this, new EventArgs());
        }

        private bool _refreshReport = false;
        public bool RefreshReport
        {
            get => _refreshReport;
            set
            {
                _refreshReport = value;
                if (value)
                {
                    OnRefreshReportAction();
                }
            }
        }

        private event EventHandler _eventHandlerRefreshReport;
        public event EventHandler EventHandlerRefreshReport
        {
            add
            {
                _eventHandlerRefreshReport += value;
            }
            remove
            {
                _eventHandlerRefreshReport -= value;
            }
        }

        private void OnRefreshReportAction()
        {
            _eventHandlerRefreshReport?.Invoke(this, new EventArgs());
        }
        #endregion

        #region Cap nhat giá trị count metal
        private int _countValue = 0;
        public int CountValue
        {
            get => _countValue;
            set
            {
                if (value != _countValue)
                {
                    _countValue = value;
                    OnCountValueAction(value);
                }
            }
        }

        private event EventHandler<CountValueChangedEventArgs> _eventHandlerCount;
        public event EventHandler<CountValueChangedEventArgs> EventHandlerCount
        {
            add
            {
                _eventHandlerCount += value;
            }
            remove
            {
                _eventHandlerCount -= value;
            }
        }

        void OnCountValueAction(int value)
        {
            _eventHandlerCount?.Invoke(this, new CountValueChangedEventArgs(value));
        }
        #endregion

        #region Scale Event
        private double _scaleValue = 0;
        public double ScaleValue
        {
            get => _scaleValue;
            set
            {
                if (value.Equals(_scaleValue))
                {
                    _scaleValue = value;
                    OnScaleValueAction(_scaleValue);
                }
            }
        }

        private event EventHandler<ScaleDynamicChangeEventArgs> _eventHandlerScale;
        public event EventHandler<ScaleDynamicChangeEventArgs> EventHandlerScale
        {
            add
            {
                _eventHandlerScale += value;
            }
            remove
            {
                _eventHandlerScale -= value;
            }
        }

        void OnScaleValueAction(double value)
        {
            _eventHandlerScale?.Invoke(this, new ScaleDynamicChangeEventArgs(value));
        }

        private int _sensorStatus = 0;
        public int SensorStatus
        {
            get => _sensorStatus;
            set
            {
                if (value.Equals(_sensorStatus))
                {
                    _sensorStatus = value;
                    OnSensorStatusAction(_sensorStatus);
                }
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerSensor;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerSensor
        {
            add
            {
                _eventHandlerSensor += value;
            }
            remove
            {
                _eventHandlerSensor -= value;
            }
        }

        void OnSensorStatusAction(int value)
        {
            _eventHandlerSensor?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion

        #region Conveyor
        private int _metalStatus = 0, _weightStatus = 0, _printStatus = 0;
        public int MetalStatus
        {
            get => _metalStatus;
            set
            {
                if (value.Equals(_metalStatus))
                {
                    _metalStatus = value;
                    OnMetalAction(_metalStatus);
                }
            }
        }

        public int WeightStatus
        {
            get => _weightStatus;
            set
            {
                if (value.Equals(_weightStatus))
                {
                    _weightStatus = value;
                    OnWeightAction(value);
                }
            }
        }

        public int PrintStatus
        {
            get => _printStatus;
            set
            {
                if (value.Equals(_printStatus))
                {
                    _printStatus = value;
                    OnPrintAction(value);
                }
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerMetal;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerMetal
        {
            add { _eventHandlerMetal += value; }
            remove { _eventHandlerMetal -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerWeight;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerWeight
        {
            add { _eventHandlerWeight += value; }
            remove { _eventHandlerWeight -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerPrint;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerPrint
        {
            add { _eventHandlerPrint += value; }
            remove { _eventHandlerPrint -= value; }
        }

        void OnMetalAction(int value)
        {
            _eventHandlerMetal?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnWeightAction(int value)
        {
            _eventHandlerMetal?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnPrintAction(int value)
        {
            _eventHandlerMetal?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion
    }

    #region Class variables for events
    public class CountValueChangedEventArgs : EventArgs
    {
        private int _countValue = 0;
        public int CountValue { get => _countValue; set => _countValue = value; }

        public CountValueChangedEventArgs(int value)
        {
            _countValue = value;
        }
    }

    public class ConveyorValueChangeEventArgs : EventArgs
    {
        private int _metalStatus = 0, _weightStatus = 0, _printStatus = 0;

        public int Metal
        {
            get => _metalStatus;
            set => _metalStatus = value;
        }
        public int WeightStatus { get => _weightStatus; set => _weightStatus = value; }
        public int PrintStatus { get => _printStatus; set => _printStatus = value; }

        public ConveyorValueChangeEventArgs(int metalStatus, int weightStatus, int printStatus)
        {
            _metalStatus = metalStatus;
            _weightStatus = weightStatus;
            _printStatus = printStatus;
        }
    }

    public class ScaleDynamicChangeEventArgs : EventArgs
    {
        private double _scaleValue = 0;
        public double ScaleValue { get => _scaleValue; set => _scaleValue = value; }

        public ScaleDynamicChangeEventArgs(double value)
        {
            _scaleValue = value;
        }
    }

    public class TagValueChangeEventArgs : EventArgs
    {
        private int _newValue = 0;
        public int NewValue { get => _newValue; set => _newValue = value; }

        public TagValueChangeEventArgs(int value)
        {
            _newValue = value;
        }
    }
    #endregion
}

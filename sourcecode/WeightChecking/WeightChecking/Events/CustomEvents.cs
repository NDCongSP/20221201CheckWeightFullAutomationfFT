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
        private double _scaleValueStable = 0;
        public double ScaleValueStable
        {
            get => _scaleValueStable;
            set
            {
                if (value != _scaleValueStable)
                {
                    _scaleValueStable = value;
                    OnScaleValueStableAction(_scaleValueStable);
                }
            }
        }

        private event EventHandler<ScaleDynamicChangeEventArgs> _eventHandlerScaleValueStable;
        public event EventHandler<ScaleDynamicChangeEventArgs> EventHandlerScaleValueStable
        {
            add
            {
                _eventHandlerScaleValueStable += value;
            }
            remove
            {
                _eventHandlerScaleValueStable -= value;
            }
        }

        void OnScaleValueStableAction(double value)
        {
            _eventHandlerScaleValueStable?.Invoke(this, new ScaleDynamicChangeEventArgs(value));
        }

        private double _scaleValue = 0;
        public double ScaleValue
        {
            get => _scaleValue;
            set
            {
                if (value != _scaleValue)
                {
                    _scaleValue = value;
                    OnScaleValueAction(value);
                }
            }
        }
        private event EventHandler<ScaleDynamicChangeEventArgs> _eventHandleScaleValue;
        public event EventHandler<ScaleDynamicChangeEventArgs> EventHandleScaleValue
        {
            add
            {
                _eventHandleScaleValue += value;
            }
            remove
            {
                _eventHandleScaleValue -= value;
            }
        }
        void OnScaleValueAction(double value)
        {
            this._eventHandleScaleValue?.Invoke(this, new ScaleDynamicChangeEventArgs(value));
        }

        private int _stableScale = 0;
        public int StableScale
        {
            get => _stableScale;
            set
            {
                if (value != _stableScale)
                {
                    _stableScale = value;
                    OnStableScaleAction(_stableScale);
                }
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerStatbleScale;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerStableScale
        {
            add
            {
                _eventHandlerStatbleScale += value;
            }
            remove
            {
                _eventHandlerStatbleScale -= value;
            }
        }

        void OnStableScaleAction(int value)
        {
            _eventHandlerStatbleScale?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion

        #region Conveyor
        private int _metalPusher = 0, _weightPusher = 0, _printPusher = 0, _sensorBeforMetalScan = 0, _sensorAfterMetalScan = 0, _metalCheckResult = 0;

        /// <summary>
        /// Biến báo sự kiện cho pusher metal scan.
        /// 0- metalscan; 1-can't read QR (reject); 2- no metal scan.
        /// </summary>
        public int MetalPusher
        {
            get => _metalPusher;
            set
            {
                if (value != _metalPusher)
                {
                    _metalPusher = value;
                    OnMetalPusherAction(_metalPusher);
                }
            }
        }

        /// <summary>
        /// Biến báo sự kiện cho pusher reject cân lôi.
        /// 0- cân ok; 1-cân lỗi.
        /// </summary>
        public int WeightPusher
        {
            get => _weightPusher;
            set
            {
                if (value != _weightPusher)
                {
                    _weightPusher = value;
                    OnWeightPusherAction(value);
                }
            }
        }

        /// <summary>
        /// Biến báo sự kiện cho pusher phân loại hàng sơn và FG.
        /// 0- hàng FG; 1-hàng đi sơn.
        /// </summary>
        public int PrintPusher
        {
            get => _printPusher;
            set
            {
                if (value != _printPusher)
                {
                    _printPusher = value;
                    OnPrintPusherAction(value);
                }
            }
        }

        /// <summary>
        /// Biến báo sự kiện sensor trước vị trí Scan metal có tác động.
        /// Bật bộ đếm thời gian báo ko đọc được tem QR lên.
        /// Sau khoảng thời gian này mà vẫn chua có tín hiệu từ scanner thì báo rejec.
        /// </summary>
        public int SensorBeforeMetalScan
        {
            get => _sensorBeforMetalScan;
            set
            {
                if (value != _sensorBeforMetalScan)
                {
                    _sensorBeforMetalScan = value;
                    OnSensorBeforeMetalScan(value);
                }
            }
        }

        public int SensorAfterMetalScan
        {
            get => _sensorAfterMetalScan;
            set
            {
                if (value != _sensorAfterMetalScan)
                {
                    _sensorAfterMetalScan = value;
                    OnSensorAfterMetalScan(value);
                }
            }
        }

        public int MetalCheckResult
        {
            get => _metalCheckResult;
            set
            {
                if (value != _metalCheckResult)
                {
                    _metalCheckResult = value;
                    OnMetalCheckResult(value);
                }
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerMetalPusher;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerMetalPusher
        {
            add { _eventHandlerMetalPusher += value; }
            remove { _eventHandlerMetalPusher -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerWeightPusher;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerWeightPusher
        {
            add { _eventHandlerWeightPusher += value; }
            remove { _eventHandlerWeightPusher -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerPrintPusher;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerPrintPusher
        {
            add { _eventHandlerPrintPusher += value; }
            remove { _eventHandlerPrintPusher -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandleSensorBeforeMetalScan;
        public event EventHandler<TagValueChangeEventArgs> EventHandleSensorBeforeMetalScan
        {
            add
            {
                _eventHandleSensorBeforeMetalScan += value;
            }
            remove
            {
                _eventHandleSensorBeforeMetalScan -= value;
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandleSensorAfterMetalScan;
        public event EventHandler<TagValueChangeEventArgs> EventHandleSensorAfterMetalScan
        {
            add
            {
                _eventHandleSensorAfterMetalScan += value;
            }
            remove
            {
                _eventHandleSensorAfterMetalScan -= value;
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandleMetalCheckResult;
        public event EventHandler<TagValueChangeEventArgs> EventHandleMetalCheckResult
        {
            add
            {
                _eventHandleMetalCheckResult += value;
            }
            remove
            {
                _eventHandleMetalCheckResult -= value;
            }
        }

        void OnMetalPusherAction(int value)
        {
            _eventHandlerMetalPusher?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnWeightPusherAction(int value)
        {
            _eventHandlerMetalPusher?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnPrintPusherAction(int value)
        {
            _eventHandlerMetalPusher?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnSensorBeforeMetalScan(int value)
        {
            _eventHandleSensorBeforeMetalScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnSensorAfterMetalScan(int value)
        {
            _eventHandleSensorAfterMetalScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnMetalCheckResult(int value)
        {
            _eventHandleMetalCheckResult?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion

        #region Event PLC on/off light
        private bool _statusLightPLC = false;
        public bool StatusLightPLC
        {
            get => _statusLightPLC;
            set
            {
                _statusLightPLC = value;
                OnStatusLightPlcAction(value);
            }
        }

        private event EventHandler<CountValueChangedEventArgs> _eventHandleStatusLightPLC;
        public event EventHandler<CountValueChangedEventArgs> EventHandleStatusLightPLC
        {
            add
            {
                _eventHandleStatusLightPLC += value;
            }
            remove
            {
                _eventHandleStatusLightPLC -= value;
            }
        }

        void OnStatusLightPlcAction(bool value)
        {
            _eventHandleStatusLightPLC?.Invoke(this, new CountValueChangedEventArgs(value));
        }
        #endregion
    }

    #region Class variables for events
    public class CountValueChangedEventArgs : EventArgs
    {
        private int _countValue = 0;
        private bool _statusLight = false;
        public int CountValue { get => _countValue; set => _countValue = value; }

        public bool StatusLight { get => _statusLight; set => _statusLight = value; }

        public CountValueChangedEventArgs(int value)
        {
            _countValue = value;
        }

        public CountValueChangedEventArgs(bool value)
        {
            _statusLight = value;
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

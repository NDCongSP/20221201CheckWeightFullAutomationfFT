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
        private int _metalPusher = 0, _metalPusher1 = 0, _weightPusher = 0, _printPusher = 0;
        private int _sensorBeforeMetalScan = 0, _sensorAfterMetalScan = 0, _metalCheckResult = 0, _sensorBeforeWeightScan = 0, _sensorAfterWeightScan = 0, _sensorMiddleMetal = 0, _sensorAfterPrintScannerFG = 0, _sensorAfterPrintScannerPrinting = 0;

        /// <summary>
        /// Biến báo sự kiện cho pusher metal scan.
        /// 0- metalscan; 1-can't read QR (reject); 2- no metal scan.
        /// </summary>
        public int MetalPusher
        {
            get => _metalPusher;
            set
            {
                //if (value != _metalPusher)
                {
                    _metalPusher = value;
                    OnMetalPusherAction(_metalPusher);
                }
            }
        }

        public int MetalPusher1
        {
            get => _metalPusher1;
            set
            {
                if (value != _metalPusher1)
                {
                    _metalPusher1 = value;
                    OnMetalPusher1Action(value);
                }
            }
        }


        /// <summary>
        /// Biến này dùng để báo có thùng vào trạm cân, để set thời gian đếm nhận tín hiệu scanner.
        /// Nếu sau thời gian này mà ko có tín hiệu từ scanner thì báo reject weight.
        /// </summary>
        public int SensorBeforeWeightScan
        {
            get => _sensorBeforeWeightScan;
            set
            {
                if (value != _sensorBeforeWeightScan)
                {
                    _sensorBeforeWeightScan = value;
                    OnSensorBeforeWeightScanAction(value);
                }
            }
        }

        public int SensorAfterWeightScan
        {
            get => _sensorAfterWeightScan;
            set
            {
                if (value != _sensorAfterWeightScan)
                {
                    _sensorAfterWeightScan = value;
                    OnSensorAfterWeightScanAction(value);
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
                _weightPusher = value;
                OnWeightPusherAction(value);
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
            get => _sensorBeforeMetalScan;
            set
            {
                if (value != _sensorBeforeMetalScan)
                {
                    _sensorBeforeMetalScan = value;
                    OnSensorBeforeMetalScanAction(value);
                }
            }
        }

        public int SensorMiddleMetal
        {
            get => _sensorMiddleMetal;
            set
            {
                if (value != _sensorMiddleMetal)
                {
                    _sensorMiddleMetal = value;
                    OnSensorMiddleMetalAction(value);
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
                    OnSensorAfterMetalScanAction(value);
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
                    OnMetalCheckResultAction(value);
                }
            }
        }

        public int SensorAfterPrintScannerFG
        {
            get => _sensorAfterPrintScannerFG;
            set
            {
                if (value != _sensorAfterPrintScannerFG)
                {
                    _sensorAfterPrintScannerFG = value;
                    OnSensorAfterPrintScannerAction(value);
                }
            }
        }
        public int SensorAfterPrintScannerPrinting
        {
            get => _sensorAfterPrintScannerPrinting;
            set
            {
                if (value != _sensorAfterPrintScannerPrinting)
                {
                    _sensorAfterPrintScannerPrinting = value;
                    OnSensorAfterPrintScannerAction(value);
                }
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerMetalPusher;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerMetalPusher
        {
            add { _eventHandlerMetalPusher += value; }
            remove { _eventHandlerMetalPusher -= value; }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandleMetalPusher1;
        public event EventHandler<TagValueChangeEventArgs> EventHandleMetalePusher1
        {
            add
            {
                _eventHandleMetalPusher1 += value;
            }
            remove
            {
                _eventHandleMetalPusher1 -= value;
            }
        }


        private event EventHandler<TagValueChangeEventArgs> _eventHandleSensorBeforeWeightScan;
        public event EventHandler<TagValueChangeEventArgs> EventHandleSensorBeforeWeightScan
        {
            add
            {
                _eventHandleSensorBeforeWeightScan += value;
            }
            remove
            {
                _eventHandleSensorBeforeWeightScan -= value;
            }
        }
        private event EventHandler<TagValueChangeEventArgs> _eventHandleSensorAfterWeightScan;
        public event EventHandler<TagValueChangeEventArgs> EventHandleSensorAfterWeightScan
        {
            add
            {
                _eventHandleSensorAfterWeightScan += value;
            }
            remove
            {
                _eventHandleSensorAfterWeightScan -= value;
            }
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

        private event EventHandler<TagValueChangeEventArgs> _eventHandlerSensorAfterPrintScanner;
        public event EventHandler<TagValueChangeEventArgs> EventHandlerSensorAfterPrintScanner
        {
            add { _eventHandlerSensorAfterPrintScanner += value; }
            remove { _eventHandlerSensorAfterPrintScanner -= value; }
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
        private event EventHandler<TagValueChangeEventArgs> _eventHandleSensorMiddleMetal;
        public event EventHandler<TagValueChangeEventArgs> EventHandleSensorMiddleMetal
        {
            add
            {
                _eventHandleSensorMiddleMetal += value;
            }
            remove
            {
                _eventHandleSensorMiddleMetal -= value;
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
        void OnMetalPusher1Action(int value)
        {
            _eventHandleMetalPusher1?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnSensorBeforeWeightScanAction(int value)
        {
            _eventHandleSensorBeforeWeightScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnSensorAfterWeightScanAction(int value)
        {
            _eventHandleSensorAfterWeightScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnWeightPusherAction(int value)
        {
            _eventHandlerWeightPusher?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnPrintPusherAction(int value)
        {
            _eventHandlerPrintPusher?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnSensorBeforeMetalScanAction(int value)
        {
            _eventHandleSensorBeforeMetalScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnSensorMiddleMetalAction(int value)
        {
            _eventHandleSensorMiddleMetal?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        void OnSensorAfterMetalScanAction(int value)
        {
            _eventHandleSensorAfterMetalScan?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        void OnMetalCheckResultAction(int value)
        {
            _eventHandleMetalCheckResult?.Invoke(this, new TagValueChangeEventArgs(value));
        }

        private void OnSensorAfterPrintScannerAction(int value)
        {
            _eventHandlerSensorAfterPrintScanner?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion

        #region Event PLC on/off light
        private int _statusLightPLC = 0;
        public int StatusLightPLC
        {
            get => _statusLightPLC;
            set
            {
                _statusLightPLC = value;
                OnStatusLightPlcAction(value);
            }
        }

        private event EventHandler<TagValueChangeEventArgs> _eventHandleStatusLightPLC;
        public event EventHandler<TagValueChangeEventArgs> EventHandleStatusLightPLC
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

        void OnStatusLightPlcAction(int value)
        {
            _eventHandleStatusLightPLC?.Invoke(this, new TagValueChangeEventArgs(value));
        }
        #endregion
    }

    #region Class variables for events
    public class CountValueChangedEventArgs : EventArgs
    {
        private int _statusLight = 0;

        public int StatusLight { get => _statusLight; set => _statusLight = value; }

        public CountValueChangedEventArgs(int value)
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

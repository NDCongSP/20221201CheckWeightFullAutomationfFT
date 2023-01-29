using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class RefreshEvent
    {
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
        public bool RefreshReport {
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
        public event  EventHandler EventHandlerRefreshReport {
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
    }

    public class CountValueChangedEventArgs : EventArgs
    {
        private int _countValue = 0;
        public int CountValue { get => _countValue; set => _countValue = value; }

        public CountValueChangedEventArgs(int value)
        {
            _countValue = value;
        }
    }
}

using System;

namespace CognexLibrary
{
    public class DataEvent
    {
        #region QR code value change
        private string _qrCodeValue = string.Empty;
        public string QRCodeValue
        {
            get => _qrCodeValue;
            set
            {
                //if (_qrCodeValue != value)
                {
                    var oldValue = _qrCodeValue;
                    _qrCodeValue = value;
                    OnChangeValueAction(oldValue, _qrCodeValue);
                }
            }
        }

        private event EventHandler<ValueChangeEventArgs> _eventHandleValueChange;
        public event EventHandler<ValueChangeEventArgs> EventHandleValueChange
        {
            add
            {
                _eventHandleValueChange += value;
            }
            remove
            {
                _eventHandleValueChange -= value;
            }
        }

        void OnChangeValueAction(string oldValue, string newValue)
        {
            _eventHandleValueChange?.Invoke(this, new ValueChangeEventArgs(oldValue, newValue));
        }
        #endregion

        #region Status change        
        private string _status = string.Empty;
        private Exception _exception = null;
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnChangeStatusAction(_status);
                }
            }
        }

        public Exception ExceptionLog
        {
            get => _exception;
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                }
            }
        }

        private void OnChangeStatusAction(string status)
        {
            this._eventHandleStatusChange?.Invoke(this, new StatusChangeEventArgs(status, _exception));
        }

        private event EventHandler<StatusChangeEventArgs> _eventHandleStatusChange;
        public event EventHandler<StatusChangeEventArgs> EventHandleStatusChange
        {
            add
            {
                _eventHandleStatusChange += value;
            }
            remove
            {
                _eventHandleStatusChange -= value;
            }
        }
        #endregion
    }

    public class ValueChangeEventArgs : EventArgs
    {
        private string _oldValue, _newValue;

        public ValueChangeEventArgs(string oldValue, string newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public string OldValue
        {
            get { return _oldValue; }
            set { _oldValue = value; }
        }

        public string NewValue
        {
            get { return _newValue; }
            set { _newValue = value; }
        }
    }

    public class StatusChangeEventArgs : EventArgs
    {
        private string _status;
        private Exception _exception;

        public StatusChangeEventArgs(string status, Exception exception)
        {
            _status = status;
            _exception = exception;
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public Exception Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }
    }
}
using System;
using UnityTimer;

namespace ModularFramework.Commons
{
    /// <summary>
    /// when flip is set to true, reset to false once preset time reached.<br/> Get() will not flip the boolean value.<br/>Must Dispose() after use
    /// </summary>
    public class TimedFlip : Flip,IDisposable
    {
        CountdownTimer _timer;
        public TimedFlip(float time)
        {
            _timer = new CountdownTimer(time);
            _timer.OnTimerStop += Reset;
        }
        public override bool Get() {
            return value;
        }

        public override void Set(bool value)
        {
            if(this.value == value) return;
            if (value)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
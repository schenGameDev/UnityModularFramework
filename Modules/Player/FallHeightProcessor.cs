using System;
using ModularFramework;

public class FallHeightProcessor : IResetable
{
    public Action<float> OnFallHeight;
    
    private readonly PlayerMoveView.JumpState _recordState;
    private readonly bool _isEnterRecordState;
    
    private PlayerMoveView.JumpState _lastState;
    private float _fallHeight;
    private bool _initialized;
    
    /// <summary>
    /// Initialize the fall height processor, should be called when player starts to fall, and pass in the JumpState used to track fall height, so it can calculate damage when landing
    /// </summary>
    /// <param name="recordState">JumpState identifying start and end of fall</param>
    /// <param name="isEnterRecordState">entering recordState is the start of fall, exiting it is the end</param>
    public FallHeightProcessor(PlayerMoveView.JumpState recordState, bool isEnterRecordState)
    {
        _recordState = recordState;
        _isEnterRecordState = isEnterRecordState;
    }
    
    
    public void Update(PlayerMoveView.JumpState newState, float height)
    {
        if (!_initialized)
        {
            // uninitialized
            _lastState = newState;
            if ((_isEnterRecordState && _lastState != _recordState)
                || (!_isEnterRecordState && _lastState == _recordState))
            {
                _initialized = true;
            }
            return;
        }
        
        if (newState != _recordState && newState == _lastState)
        {
            if (_isEnterRecordState)
            {
                // end of fall
                OnFallHeight?.Invoke(_fallHeight - height);
            }
            else
            {
                // start of fall
                _fallHeight = height;
            }
        }
        
        if (newState == _recordState && _lastState != newState)
        {
            if (_isEnterRecordState)
            {
                // start of fall
                _fallHeight = height;
            }
            else
            {
                // end of fall
                OnFallHeight?.Invoke(_fallHeight - height);
            }
        }
        _lastState = newState;
    }

    public void ResetState()
    {
        _lastState = _recordState;
        _fallHeight = 0;
        _initialized  = false;
    }
}
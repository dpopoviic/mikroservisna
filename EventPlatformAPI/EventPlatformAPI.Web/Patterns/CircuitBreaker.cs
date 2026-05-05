namespace EventPlatformAPI.Web.Patterns
{
    public enum CircuitBreakerState
    {
        Closed, 
        Open, 
        HalfOpen
    }
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string? message) : base(message)
        {
        }
    }
    public class CircuitBreaker
    {
        private object _lock = new object();
        private readonly int _failureTrashold;
        private int _failureCount;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly TimeSpan _openDuration;
        private DateTime _lastFailureTime = DateTime.MinValue;

        public CircuitBreaker(int failureTrashold, TimeSpan openDuration)
        {
            _failureTrashold = failureTrashold;
            _openDuration = openDuration;
        }

        public CircuitBreakerState State
        {
            get 
            { 
                lock (_lock)
                {
                    if(_state == CircuitBreakerState.Open && (DateTime.UtcNow - _lastFailureTime) > _openDuration)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                    }
                }
                
                return _state; 
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if(State == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("CircuitBreaker Open Exception");
            }
            try
            {
                var res = await action();
                lock (_lock)
                {
                    _failureCount = 0;
                    _state = CircuitBreakerState.Closed;
                }
                return res;
            }
            catch (Exception)
            {
                lock (_lock) 
                {
                    _failureCount++;
                    _lastFailureTime = DateTime.UtcNow;
                    if(_state == CircuitBreakerState.HalfOpen)
                    {
                        _state = CircuitBreakerState.Open;
                    }
                    if(_failureTrashold < _failureCount)
                    {
                        _state = CircuitBreakerState.Open;
                    }

                }
                throw;
            }
        }
    }
}

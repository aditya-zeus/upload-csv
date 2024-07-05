using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Task1.Services
{
    // Primary Constructor
    public class RetryPolicies()
    {
        public static IAsyncPolicy GetRetryPolicy(int retry = 3) {
            var retryPolicy = Policy.Handle<Exception>()
                                .RetryAsync(retry, (exception, retryCount) => {
                                    Console.WriteLine($"{retryCount}: Excpetion occured - {exception.Message}");
                                });
            return retryPolicy;
        }

        public static AsyncCircuitBreakerPolicy GetCircuitBreakerPolicy(int breakCircuit = 3) {
            var circuitBreakerPolicy = Policy.Handle<Exception>()
                                .CircuitBreakerAsync(breakCircuit, TimeSpan.FromSeconds(30));
            return circuitBreakerPolicy;
        }

        public static IAsyncPolicy GetWaitAndRetryPolicy(int retry = 3, int retryAfter = 10) {
            var retryPolicy = Policy.Handle<Exception>()
                                .WaitAndRetryAsync(retry, i => TimeSpan.FromSeconds(retryAfter * i));
            return retryPolicy;
        }
    }
}
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

internal sealed class PortableThreadPool
{
	[StructLayout(LayoutKind.Explicit, Size = 384)]
	private struct CacheLineSeparated
	{
		[FieldOffset(64)]
		public ThreadCounts counts;

		[FieldOffset(128)]
		public int lastDequeueTime;

		[FieldOffset(192)]
		public int priorCompletionCount;

		[FieldOffset(196)]
		public int priorCompletedWorkRequestsTime;

		[FieldOffset(200)]
		public int nextCompletedWorkRequestsTime;

		[FieldOffset(256)]
		public volatile int numRequestedWorkers;

		[FieldOffset(260)]
		public int gateThreadRunningState;
	}

	private enum PendingBlockingAdjustment : byte
	{
		None,
		Immediately,
		WithDelayIfNecessary
	}

	private static class BlockingConfig
	{
		public static readonly bool IsCooperativeBlockingEnabled;

		public static readonly bool IgnoreMemoryUsage;

		public static readonly short ThreadsToAddWithoutDelay;

		public static readonly short ThreadsPerDelayStep;

		public static readonly uint DelayStepMs;

		public static readonly uint MaxDelayMs;

		static BlockingConfig()
		{
			IsCooperativeBlockingEnabled = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.Blocking.CooperativeBlocking", defaultValue: true);
			IgnoreMemoryUsage = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.Blocking.IgnoreMemoryUsage", defaultValue: false);
			int int32Config = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.Blocking.ThreadsToAddWithoutDelay_ProcCountFactor", 1, allowNegative: false);
			int int32Config2 = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.Blocking.ThreadsPerDelayStep_ProcCountFactor", 1, allowNegative: false);
			DelayStepMs = (uint)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.Blocking.DelayStepMs", 25, allowNegative: false);
			MaxDelayMs = (uint)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.Blocking.MaxDelayMs", 250, allowNegative: false);
			int processorCount = Environment.ProcessorCount;
			ThreadsToAddWithoutDelay = (short)(processorCount * int32Config);
			if (ThreadsToAddWithoutDelay > short.MaxValue || ThreadsToAddWithoutDelay / processorCount != int32Config)
			{
				ThreadsToAddWithoutDelay = short.MaxValue;
			}
			int32Config2 = Math.Max(1, int32Config2);
			short num = (short)(32767 - ThreadsToAddWithoutDelay);
			ThreadsPerDelayStep = (short)(processorCount * int32Config2);
			if (ThreadsPerDelayStep > num || ThreadsPerDelayStep / processorCount != int32Config2)
			{
				ThreadsPerDelayStep = num;
			}
			MaxDelayMs = Math.Max(1u, Math.Min(MaxDelayMs, 500u));
			DelayStepMs = Math.Max(1u, Math.Min(DelayStepMs, MaxDelayMs));
		}
	}

	private static class GateThread
	{
		private struct DelayHelper
		{
			private int _previousGateActivitiesTimeMs;

			private int _previousBlockingAdjustmentDelayStartTimeMs;

			private uint _previousBlockingAdjustmentDelayMs;

			private bool _runGateActivitiesAfterNextDelay;

			private bool _adjustForBlockingAfterNextDelay;

			public bool HasBlockingAdjustmentDelay => _previousBlockingAdjustmentDelayMs != 0;

			public void SetGateActivitiesTime(int currentTimeMs)
			{
				_previousGateActivitiesTimeMs = currentTimeMs;
			}

			public void SetBlockingAdjustmentTimeAndDelay(int currentTimeMs, uint delayMs)
			{
				_previousBlockingAdjustmentDelayStartTimeMs = currentTimeMs;
				_previousBlockingAdjustmentDelayMs = delayMs;
			}

			public void ClearBlockingAdjustmentDelay()
			{
				_previousBlockingAdjustmentDelayMs = 0u;
			}

			public uint GetNextDelay(int currentTimeMs)
			{
				uint num = (uint)(currentTimeMs - _previousGateActivitiesTimeMs);
				uint num2 = ((num >= 500) ? 1u : (500 - num));
				if (_previousBlockingAdjustmentDelayMs == 0)
				{
					_runGateActivitiesAfterNextDelay = true;
					_adjustForBlockingAfterNextDelay = false;
					return num2;
				}
				uint num3 = (uint)(currentTimeMs - _previousBlockingAdjustmentDelayStartTimeMs);
				uint num4 = ((num3 >= _previousBlockingAdjustmentDelayMs) ? 1u : (_previousBlockingAdjustmentDelayMs - num3));
				uint num5 = Math.Min(num2, num4);
				_runGateActivitiesAfterNextDelay = num5 == num2;
				_adjustForBlockingAfterNextDelay = num5 == num4;
				return num5;
			}

			public bool ShouldPerformGateActivities(int currentTimeMs, bool wasSignaledToWake)
			{
				bool flag = (!wasSignaledToWake && _runGateActivitiesAfterNextDelay) || (uint)(currentTimeMs - _previousGateActivitiesTimeMs) >= 500u;
				if (flag)
				{
					SetGateActivitiesTime(currentTimeMs);
				}
				return flag;
			}

			public bool HasBlockingAdjustmentDelayElapsed(int currentTimeMs, bool wasSignaledToWake)
			{
				if (!wasSignaledToWake && _adjustForBlockingAfterNextDelay)
				{
					return true;
				}
				uint num = (uint)(currentTimeMs - _previousBlockingAdjustmentDelayStartTimeMs);
				return num >= _previousBlockingAdjustmentDelayMs;
			}
		}

		private static readonly AutoResetEvent RunGateThreadEvent = new AutoResetEvent(initialState: true);

		private static readonly AutoResetEvent DelayEvent = new AutoResetEvent(initialState: false);

		private static void GateThreadStart()
		{
			bool booleanConfig = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.DisableStarvationDetection", defaultValue: false);
			bool booleanConfig2 = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation", defaultValue: false);
			CpuUtilizationReader cpuUtilizationReader = default(CpuUtilizationReader);
			_ = cpuUtilizationReader.CurrentUtilization;
			PortableThreadPool threadPoolInstance = ThreadPoolInstance;
			LowLevelLock threadAdjustmentLock = threadPoolInstance._threadAdjustmentLock;
			DelayHelper delayHelper = default(DelayHelper);
			if (BlockingConfig.IsCooperativeBlockingEnabled && !BlockingConfig.IgnoreMemoryUsage)
			{
				threadPoolInstance.OnGen2GCCallback();
				Gen2GcCallback.Register(threadPoolInstance.OnGen2GCCallback);
			}
			while (true)
			{
				RunGateThreadEvent.WaitOne();
				int tickCount = Environment.TickCount;
				delayHelper.SetGateActivitiesTime(tickCount);
				while (true)
				{
					bool wasSignaledToWake = DelayEvent.WaitOne((int)delayHelper.GetNextDelay(tickCount));
					tickCount = Environment.TickCount;
					PendingBlockingAdjustment pendingBlockingAdjustment = threadPoolInstance._pendingBlockingAdjustment;
					if (pendingBlockingAdjustment == PendingBlockingAdjustment.None)
					{
						delayHelper.ClearBlockingAdjustmentDelay();
					}
					else
					{
						bool flag = false;
						if (delayHelper.HasBlockingAdjustmentDelay)
						{
							flag = delayHelper.HasBlockingAdjustmentDelayElapsed(tickCount, wasSignaledToWake);
							if (pendingBlockingAdjustment == PendingBlockingAdjustment.WithDelayIfNecessary && !flag)
							{
								goto IL_00f5;
							}
						}
						uint num = threadPoolInstance.PerformBlockingAdjustment(flag);
						if (num == 0)
						{
							delayHelper.ClearBlockingAdjustmentDelay();
						}
						else
						{
							delayHelper.SetBlockingAdjustmentTimeAndDelay(tickCount, num);
						}
					}
					goto IL_00f5;
					IL_00f5:
					if (!delayHelper.ShouldPerformGateActivities(tickCount, wasSignaledToWake))
					{
						continue;
					}
					if (ThreadPool.EnableWorkerTracking && NativeRuntimeEventSource.Log.IsEnabled())
					{
						NativeRuntimeEventSource.Log.ThreadPoolWorkingThreadCount((uint)threadPoolInstance.GetAndResetHighWatermarkCountOfThreadsProcessingUserCallbacks(), 0);
					}
					int currentUtilization = cpuUtilizationReader.CurrentUtilization;
					threadPoolInstance._cpuUtilization = currentUtilization;
					if (!booleanConfig && threadPoolInstance._pendingBlockingAdjustment == PendingBlockingAdjustment.None && threadPoolInstance._separated.numRequestedWorkers > 0 && SufficientDelaySinceLastDequeue(threadPoolInstance))
					{
						bool flag2 = false;
						threadAdjustmentLock.Acquire();
						try
						{
							ThreadCounts threadCounts = threadPoolInstance._separated.counts;
							while (threadCounts.NumProcessingWork < threadPoolInstance._maxThreads && threadCounts.NumProcessingWork >= threadCounts.NumThreadsGoal)
							{
								if (booleanConfig2)
								{
									Debugger.Break();
								}
								ThreadCounts newCounts = threadCounts;
								short newThreadCount = (newCounts.NumThreadsGoal = (short)(threadCounts.NumProcessingWork + 1));
								ThreadCounts threadCounts2 = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, threadCounts);
								if (threadCounts2 == threadCounts)
								{
									HillClimbing.ThreadPoolHillClimber.ForceChange(newThreadCount, HillClimbing.StateOrTransition.Starvation);
									flag2 = true;
									break;
								}
								threadCounts = threadCounts2;
							}
						}
						finally
						{
							threadAdjustmentLock.Release();
						}
						if (flag2)
						{
							WorkerThread.MaybeAddWorkingWorker(threadPoolInstance);
						}
					}
					if (threadPoolInstance._separated.numRequestedWorkers <= 0 && threadPoolInstance._pendingBlockingAdjustment == PendingBlockingAdjustment.None && Interlocked.Decrement(ref threadPoolInstance._separated.gateThreadRunningState) <= GetRunningStateForNumRuns(0))
					{
						break;
					}
				}
			}
		}

		public static void Wake(PortableThreadPool threadPoolInstance)
		{
			DelayEvent.Set();
			EnsureRunning(threadPoolInstance);
		}

		private static bool SufficientDelaySinceLastDequeue(PortableThreadPool threadPoolInstance)
		{
			uint num = (uint)(Environment.TickCount - threadPoolInstance._separated.lastDequeueTime);
			uint num2 = ((threadPoolInstance._cpuUtilization >= 80) ? ((uint)(threadPoolInstance._separated.counts.NumThreadsGoal * 1000)) : 500u);
			return num > num2;
		}

		internal static void EnsureRunning(PortableThreadPool threadPoolInstance)
		{
			if (threadPoolInstance._separated.gateThreadRunningState != GetRunningStateForNumRuns(2))
			{
				EnsureRunningSlow(threadPoolInstance);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void EnsureRunningSlow(PortableThreadPool threadPoolInstance)
		{
			int num = Interlocked.Exchange(ref threadPoolInstance._separated.gateThreadRunningState, GetRunningStateForNumRuns(2));
			if (num == GetRunningStateForNumRuns(0))
			{
				RunGateThreadEvent.Set();
			}
			else if ((num & 4) == 0)
			{
				CreateGateThread();
			}
		}

		private static int GetRunningStateForNumRuns(int numRuns)
		{
			return 4 | numRuns;
		}

		private static void CreateGateThread()
		{
			try
			{
				Thread thread = new Thread(GateThreadStart, 262144)
				{
					IsThreadPoolThread = true,
					IsBackground = true,
					Name = ".NET TP Gate"
				};
				thread.UnsafeStart();
			}
			catch (Exception exception)
			{
				Environment.FailFast("Failed to create the thread pool Gate thread.", exception);
			}
		}
	}

	private sealed class HillClimbing
	{
		public enum StateOrTransition
		{
			Warmup,
			Initializing,
			RandomMove,
			ClimbingMove,
			ChangePoint,
			Stabilizing,
			Starvation,
			ThreadTimedOut,
			CooperativeBlocking
		}

		private struct LogEntry
		{
			public int tickCount;

			public StateOrTransition stateOrTransition;

			public int newControlSetting;

			public int lastHistoryCount;

			public float lastHistoryMean;
		}

		private readonly struct Complex
		{
			public double Imaginary { get; }

			public double Real { get; }

			public Complex(double real, double imaginary)
			{
				Real = real;
				Imaginary = imaginary;
			}

			public static Complex operator *(double scalar, Complex complex)
			{
				return new Complex(scalar * complex.Real, scalar * complex.Imaginary);
			}

			public static Complex operator /(Complex complex, double scalar)
			{
				return new Complex(complex.Real / scalar, complex.Imaginary / scalar);
			}

			public static Complex operator -(Complex lhs, Complex rhs)
			{
				return new Complex(lhs.Real - rhs.Real, lhs.Imaginary - rhs.Imaginary);
			}

			public static Complex operator /(Complex lhs, Complex rhs)
			{
				double num = rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary;
				return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) / num, ((0.0 - lhs.Real) * rhs.Imaginary + lhs.Imaginary * rhs.Real) / num);
			}

			public double Abs()
			{
				return Math.Sqrt(Real * Real + Imaginary * Imaginary);
			}
		}

		public static readonly bool IsDisabled = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.HillClimbing.Disable", defaultValue: false);

		public static readonly HillClimbing ThreadPoolHillClimber = new HillClimbing();

		private readonly int _wavePeriod;

		private readonly int _samplesToMeasure;

		private readonly double _targetThroughputRatio;

		private readonly double _targetSignalToNoiseRatio;

		private readonly double _maxChangePerSecond;

		private readonly double _maxChangePerSample;

		private readonly int _maxThreadWaveMagnitude;

		private readonly int _sampleIntervalMsLow;

		private readonly double _threadMagnitudeMultiplier;

		private readonly int _sampleIntervalMsHigh;

		private readonly double _throughputErrorSmoothingFactor;

		private readonly double _gainExponent;

		private readonly double _maxSampleError;

		private double _currentControlSetting;

		private long _totalSamples;

		private int _lastThreadCount;

		private double _averageThroughputNoise;

		private double _secondsElapsedSinceLastChange;

		private double _completionsSinceLastChange;

		private int _accumulatedCompletionCount;

		private double _accumulatedSampleDurationSeconds;

		private readonly double[] _samples;

		private readonly double[] _threadCounts;

		private int _currentSampleMs;

		private readonly Random.XoshiroImpl _randomIntervalGenerator = new Random.XoshiroImpl();

		private readonly LogEntry[] _log = new LogEntry[200];

		private int _logStart;

		private int _logSize;

		public HillClimbing()
		{
			_wavePeriod = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WavePeriod", 4, allowNegative: false);
			_maxThreadWaveMagnitude = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxWaveMagnitude", 20, allowNegative: false);
			_threadMagnitudeMultiplier = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WaveMagnitudeMultiplier", 100, allowNegative: false) / 100.0;
			_samplesToMeasure = _wavePeriod * AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WaveHistorySize", 8, allowNegative: false);
			_targetThroughputRatio = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.Bias", 15, allowNegative: false) / 100.0;
			_targetSignalToNoiseRatio = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.TargetSignalToNoiseRatio", 300, allowNegative: false) / 100.0;
			_maxChangePerSecond = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxChangePerSecond", 4, allowNegative: false);
			_maxChangePerSample = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxChangePerSample", 20, allowNegative: false);
			int int32Config = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.SampleIntervalLow", 10, allowNegative: false);
			int int32Config2 = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.SampleIntervalHigh", 200, allowNegative: false);
			if (int32Config <= int32Config2)
			{
				_sampleIntervalMsLow = int32Config;
				_sampleIntervalMsHigh = int32Config2;
			}
			else
			{
				_sampleIntervalMsLow = 10;
				_sampleIntervalMsHigh = 200;
			}
			_throughputErrorSmoothingFactor = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.ErrorSmoothingFactor", 1, allowNegative: false) / 100.0;
			_gainExponent = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.GainExponent", 200, allowNegative: false) / 100.0;
			_maxSampleError = (double)AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxSampleErrorPercent", 15, allowNegative: false) / 100.0;
			_samples = new double[_samplesToMeasure];
			_threadCounts = new double[_samplesToMeasure];
			_currentSampleMs = _randomIntervalGenerator.Next(_sampleIntervalMsLow, _sampleIntervalMsHigh + 1);
		}

		public (int newThreadCount, int newSampleMs) Update(int currentThreadCount, double sampleDurationSeconds, int numCompletions)
		{
			if (currentThreadCount != _lastThreadCount)
			{
				ForceChange(currentThreadCount, StateOrTransition.Initializing);
			}
			_secondsElapsedSinceLastChange += sampleDurationSeconds;
			_completionsSinceLastChange += numCompletions;
			sampleDurationSeconds += _accumulatedSampleDurationSeconds;
			numCompletions += _accumulatedCompletionCount;
			if (_totalSamples > 0 && ((double)currentThreadCount - 1.0) / (double)numCompletions >= _maxSampleError)
			{
				_accumulatedSampleDurationSeconds = sampleDurationSeconds;
				_accumulatedCompletionCount = numCompletions;
				return (newThreadCount: currentThreadCount, newSampleMs: 10);
			}
			_accumulatedSampleDurationSeconds = 0.0;
			_accumulatedCompletionCount = 0;
			double num = (double)numCompletions / sampleDurationSeconds;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentSample(num, 0);
			}
			int num2 = (int)(_totalSamples % _samplesToMeasure);
			_samples[num2] = num;
			_threadCounts[num2] = currentThreadCount;
			_totalSamples++;
			Complex complex = default(Complex);
			Complex complex2 = default(Complex);
			double num3 = 0.0;
			Complex complex3 = default(Complex);
			double num4 = 0.0;
			StateOrTransition state = StateOrTransition.Warmup;
			int num5 = (int)Math.Min(_totalSamples - 1, _samplesToMeasure) / _wavePeriod * _wavePeriod;
			if (num5 > _wavePeriod)
			{
				double num6 = 0.0;
				double num7 = 0.0;
				for (int i = 0; i < num5; i++)
				{
					num6 += _samples[(_totalSamples - num5 + i) % _samplesToMeasure];
					num7 += _threadCounts[(_totalSamples - num5 + i) % _samplesToMeasure];
				}
				double num8 = num6 / (double)num5;
				double num9 = num7 / (double)num5;
				if (num8 > 0.0 && num9 > 0.0)
				{
					double period = (double)num5 / ((double)num5 / (double)_wavePeriod + 1.0);
					double num10 = (double)num5 / ((double)num5 / (double)_wavePeriod - 1.0);
					complex2 = GetWaveComponent(_samples, num5, _wavePeriod) / num8;
					num3 = (GetWaveComponent(_samples, num5, period) / num8).Abs();
					if (num10 <= (double)num5)
					{
						num3 = Math.Max(num3, (GetWaveComponent(_samples, num5, num10) / num8).Abs());
					}
					complex = GetWaveComponent(_threadCounts, num5, _wavePeriod) / num9;
					if (_averageThroughputNoise == 0.0)
					{
						_averageThroughputNoise = num3;
					}
					else
					{
						_averageThroughputNoise = _throughputErrorSmoothingFactor * num3 + (1.0 - _throughputErrorSmoothingFactor) * _averageThroughputNoise;
					}
					if (complex.Abs() > 0.0)
					{
						complex3 = (complex2 - _targetThroughputRatio * complex) / complex;
						state = StateOrTransition.ClimbingMove;
					}
					else
					{
						complex3 = new Complex(0.0, 0.0);
						state = StateOrTransition.Stabilizing;
					}
					double num11 = Math.Max(_averageThroughputNoise, num3);
					num4 = ((!(num11 > 0.0)) ? 1.0 : (complex.Abs() / num11 / _targetSignalToNoiseRatio));
				}
			}
			double num12 = Math.Min(1.0, Math.Max(-1.0, complex3.Real));
			num12 *= Math.Min(1.0, Math.Max(0.0, num4));
			double num13 = _maxChangePerSecond * sampleDurationSeconds;
			num12 = Math.Pow(Math.Abs(num12), _gainExponent) * (double)((num12 >= 0.0) ? 1 : (-1)) * num13;
			num12 = Math.Min(num12, _maxChangePerSample);
			PortableThreadPool threadPoolInstance = ThreadPoolInstance;
			if (num12 > 0.0 && threadPoolInstance._cpuUtilization > 95)
			{
				num12 = 0.0;
			}
			_currentControlSetting += num12;
			int val = (int)(0.5 + _currentControlSetting * _averageThroughputNoise * _targetSignalToNoiseRatio * _threadMagnitudeMultiplier * 2.0);
			val = Math.Min(val, _maxThreadWaveMagnitude);
			val = Math.Max(val, 1);
			int maxThreads = threadPoolInstance._maxThreads;
			int minThreadsGoal = threadPoolInstance.MinThreadsGoal;
			_currentControlSetting = Math.Min(maxThreads - val, _currentControlSetting);
			_currentControlSetting = Math.Max(minThreadsGoal, _currentControlSetting);
			int val2 = (int)(_currentControlSetting + (double)(val * (_totalSamples / (_wavePeriod / 2) % 2)));
			val2 = Math.Min(maxThreads, val2);
			val2 = Math.Max(minThreadsGoal, val2);
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentStats(sampleDurationSeconds, num, complex.Real, complex2.Real, num3, _averageThroughputNoise, complex3.Real, num4, _currentControlSetting, (ushort)val, 0);
			}
			if (val2 != currentThreadCount)
			{
				ChangeThreadCount(val2, state);
				_secondsElapsedSinceLastChange = 0.0;
				_completionsSinceLastChange = 0.0;
			}
			int item = ((!(complex3.Real < 0.0) || val2 != minThreadsGoal) ? _currentSampleMs : ((int)(0.5 + (double)_currentSampleMs * (10.0 * Math.Min(0.0 - complex3.Real, 1.0)))));
			return (newThreadCount: val2, newSampleMs: item);
		}

		private void ChangeThreadCount(int newThreadCount, StateOrTransition state)
		{
			_lastThreadCount = newThreadCount;
			if (state != StateOrTransition.CooperativeBlocking)
			{
				_currentSampleMs = _randomIntervalGenerator.Next(_sampleIntervalMsLow, _sampleIntervalMsHigh + 1);
			}
			double throughput = ((_secondsElapsedSinceLastChange > 0.0) ? (_completionsSinceLastChange / _secondsElapsedSinceLastChange) : 0.0);
			LogTransition(newThreadCount, throughput, state);
		}

		private void LogTransition(int newThreadCount, double throughput, StateOrTransition stateOrTransition)
		{
			int num = (_logStart + _logSize) % 200;
			if (_logSize == 200)
			{
				_logStart = (_logStart + 1) % 200;
				_logSize--;
			}
			ref LogEntry reference = ref _log[num];
			reference.tickCount = Environment.TickCount;
			reference.stateOrTransition = stateOrTransition;
			reference.newControlSetting = newThreadCount;
			reference.lastHistoryCount = (int)(Math.Min(_totalSamples, _samplesToMeasure) / _wavePeriod) * _wavePeriod;
			reference.lastHistoryMean = (float)throughput;
			_logSize++;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentAdjustment(throughput, (uint)newThreadCount, (NativeRuntimeEventSource.ThreadAdjustmentReasonMap)stateOrTransition, 0);
			}
		}

		public void ForceChange(int newThreadCount, StateOrTransition state)
		{
			if (_lastThreadCount != newThreadCount)
			{
				_currentControlSetting += newThreadCount - _lastThreadCount;
				ChangeThreadCount(newThreadCount, state);
			}
		}

		private Complex GetWaveComponent(double[] samples, int numSamples, double period)
		{
			double num = Math.PI * 2.0 / period;
			double num2 = Math.Cos(num);
			double num3 = 2.0 * num2;
			double num4 = 0.0;
			double num5 = 0.0;
			for (int i = 0; i < numSamples; i++)
			{
				double num6 = num3 * num4 - num5 + samples[(_totalSamples - numSamples + i) % _samplesToMeasure];
				num5 = num4;
				num4 = num6;
			}
			return new Complex(num4 - num5 * num2, num5 * Math.Sin(num)) / numSamples;
		}
	}

	private sealed class IOCompletionPoller
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct Callback : IThreadPoolTypedWorkItemQueueCallback<Event>
		{
			public unsafe static void Invoke(Event e)
			{
				if (NativeRuntimeEventSource.Log.IsEnabled())
				{
					NativeRuntimeEventSource.Log.ThreadPoolIODequeue(e.nativeOverlapped);
				}
				uint num = (uint)e.nativeOverlapped->InternalLow;
				uint errorCode = 0u;
				if (!Interop.StatusOptions.NT_SUCCESS(num))
				{
					errorCode = Interop.NtDll.RtlNtStatusToDosError((int)num);
				}
				IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, e.bytesTransferred, e.nativeOverlapped);
			}
		}

		private readonly struct Event
		{
			public unsafe readonly NativeOverlapped* nativeOverlapped;

			public readonly uint bytesTransferred;

			public unsafe Event(NativeOverlapped* nativeOverlapped, uint bytesTransferred)
			{
				this.nativeOverlapped = nativeOverlapped;
				this.bytesTransferred = bytesTransferred;
			}
		}

		private readonly nint _port;

		private unsafe readonly Interop.Kernel32.OVERLAPPED_ENTRY* _nativeEvents;

		private readonly ThreadPoolTypedWorkItemQueue<Event, Callback> _events;

		private readonly Thread _thread;

		public unsafe IOCompletionPoller(nint port)
		{
			_port = port;
			if (!UnsafeInlineIOCompletionCallbacks)
			{
				_nativeEvents = (Interop.Kernel32.OVERLAPPED_ENTRY*)NativeMemory.Alloc(1024u, (nuint)sizeof(Interop.Kernel32.OVERLAPPED_ENTRY));
				_events = new ThreadPoolTypedWorkItemQueue<Event, Callback>();
				_thread = new Thread(Poll, 262144);
				if (IOCompletionPollerCount * 4 < Environment.ProcessorCount)
				{
					_thread.Priority = ThreadPriority.AboveNormal;
				}
			}
			else
			{
				_thread = new Thread(PollAndInlineCallbacks);
			}
			_thread.IsThreadPoolThread = true;
			_thread.IsBackground = true;
			_thread.Name = ".NET ThreadPool IO";
			_thread.UnsafeStart();
		}

		private unsafe void Poll()
		{
			int ulNumEntriesRemoved;
			while (Interop.Kernel32.GetQueuedCompletionStatusEx(_port, _nativeEvents, 1024, out ulNumEntriesRemoved, -1, fAlertable: false))
			{
				for (int i = 0; i < ulNumEntriesRemoved; i++)
				{
					Interop.Kernel32.OVERLAPPED_ENTRY* ptr = _nativeEvents + i;
					if (ptr->lpOverlapped != null)
					{
						_events.BatchEnqueue(new Event(ptr->lpOverlapped, ptr->dwNumberOfBytesTransferred));
					}
				}
				_events.CompleteBatchEnqueue();
			}
			ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
		}

		private unsafe void PollAndInlineCallbacks()
		{
			while (true)
			{
				uint errorCode = 0u;
				if (!Interop.Kernel32.GetQueuedCompletionStatus(_port, out var lpNumberOfBytesTransferred, out var _, out var lpOverlapped, -1))
				{
					errorCode = (uint)Marshal.GetLastPInvokeError();
				}
				NativeOverlapped* ptr = (NativeOverlapped*)lpOverlapped;
				if (ptr != null)
				{
					if (NativeRuntimeEventSource.Log.IsEnabled())
					{
						NativeRuntimeEventSource.Log.ThreadPoolIODequeue(ptr);
					}
					IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, lpNumberOfBytesTransferred, ptr);
				}
			}
		}
	}

	private struct ThreadCounts : IEquatable<ThreadCounts>
	{
		private ulong _data;

		public short NumProcessingWork
		{
			get
			{
				return GetInt16Value(0);
			}
			set
			{
				SetInt16Value(Math.Max((short)0, value), 0);
			}
		}

		public short NumExistingThreads
		{
			get
			{
				return GetInt16Value(16);
			}
			set
			{
				SetInt16Value(Math.Max((short)0, value), 16);
			}
		}

		public short NumThreadsGoal
		{
			get
			{
				return GetInt16Value(32);
			}
			set
			{
				SetInt16Value(Math.Max((short)1, value), 32);
			}
		}

		private ThreadCounts(ulong data)
		{
			_data = data;
		}

		private short GetInt16Value(byte shift)
		{
			return (short)(_data >> (int)shift);
		}

		private void SetInt16Value(short value, byte shift)
		{
			_data = (_data & (ulong)(~(65535L << (int)shift))) | ((ulong)(ushort)value << (int)shift);
		}

		public ThreadCounts InterlockedSetNumThreadsGoal(short value)
		{
			ThreadCounts threadCounts = this;
			ThreadCounts threadCounts2;
			while (true)
			{
				threadCounts2 = threadCounts;
				threadCounts2.NumThreadsGoal = value;
				ThreadCounts threadCounts3 = InterlockedCompareExchange(threadCounts2, threadCounts);
				if (threadCounts3 == threadCounts)
				{
					break;
				}
				threadCounts = threadCounts3;
			}
			return threadCounts2;
		}

		public ThreadCounts VolatileRead()
		{
			return new ThreadCounts(Volatile.Read(ref _data));
		}

		public ThreadCounts InterlockedCompareExchange(ThreadCounts newCounts, ThreadCounts oldCounts)
		{
			return new ThreadCounts(Interlocked.CompareExchange(ref _data, newCounts._data, oldCounts._data));
		}

		public static bool operator ==(ThreadCounts lhs, ThreadCounts rhs)
		{
			return lhs._data == rhs._data;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is ThreadCounts other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(ThreadCounts other)
		{
			return _data == other._data;
		}

		public override int GetHashCode()
		{
			return (int)_data + (int)(_data >> 32);
		}
	}

	private sealed class WaitThreadNode
	{
		public WaitThread Thread { get; }

		public WaitThreadNode Next { get; set; }

		public WaitThreadNode(WaitThread thread)
		{
			Thread = thread;
		}
	}

	internal sealed class WaitThread
	{
		private readonly RegisteredWaitHandle[] _registeredWaits = new RegisteredWaitHandle[63];

		private readonly SafeWaitHandle[] _waitHandles = new SafeWaitHandle[64];

		private int _numUserWaits;

		private readonly RegisteredWaitHandle[] _pendingRemoves = new RegisteredWaitHandle[63];

		private int _numPendingRemoves;

		private readonly AutoResetEvent _changeHandlesEvent = new AutoResetEvent(initialState: false);

		internal bool AnyUserWaits => _numUserWaits != 0;

		public WaitThread()
		{
			_waitHandles[0] = _changeHandlesEvent.SafeWaitHandle;
			Thread thread = new Thread(WaitThreadStart, 262144)
			{
				IsThreadPoolThread = true,
				IsBackground = true,
				Name = ".NET TP Wait"
			};
			thread.UnsafeStart();
		}

		private void WaitThreadStart()
		{
			while (true)
			{
				int num = ProcessRemovals();
				int tickCount = Environment.TickCount;
				int num2 = -1;
				if (num == 0)
				{
					num2 = ThreadPoolThreadTimeoutMs;
				}
				else
				{
					for (int i = 0; i < num; i++)
					{
						RegisteredWaitHandle registeredWaitHandle = _registeredWaits[i];
						if (!registeredWaitHandle.IsInfiniteTimeout)
						{
							int num3 = Math.Max(0, registeredWaitHandle.TimeoutTimeMs - tickCount);
							num2 = ((num2 != -1) ? Math.Min(num3, num2) : num3);
							if (num2 == 0)
							{
								break;
							}
						}
					}
				}
				int num4 = WaitHandle.WaitAny(new ReadOnlySpan<SafeWaitHandle>(_waitHandles, 0, num + 1), num2);
				if (num4 >= 128 && num4 < 129 + num)
				{
					num4 += -128;
				}
				switch (num4)
				{
				case 0:
					break;
				default:
				{
					RegisteredWaitHandle registeredHandle = _registeredWaits[num4 - 1];
					QueueWaitCompletion(registeredHandle, timedOut: false);
					break;
				}
				case 258:
				{
					if (num == 0 && ThreadPoolInstance.TryRemoveWaitThread(this))
					{
						return;
					}
					tickCount = Environment.TickCount;
					for (int j = 0; j < num; j++)
					{
						RegisteredWaitHandle registeredWaitHandle2 = _registeredWaits[j];
						if (!registeredWaitHandle2.IsInfiniteTimeout && tickCount - registeredWaitHandle2.TimeoutTimeMs >= 0)
						{
							QueueWaitCompletion(registeredWaitHandle2, timedOut: true);
						}
					}
					break;
				}
				}
			}
		}

		private int ProcessRemovals()
		{
			PortableThreadPool threadPoolInstance = ThreadPoolInstance;
			threadPoolInstance._waitThreadLock.Acquire();
			try
			{
				if (_numPendingRemoves == 0 || _numUserWaits == 0)
				{
					return _numUserWaits;
				}
				int numUserWaits = _numUserWaits;
				int numPendingRemoves = _numPendingRemoves;
				for (int i = 0; i < _numPendingRemoves; i++)
				{
					RegisteredWaitHandle registeredWaitHandle = _pendingRemoves[i];
					int numUserWaits2 = _numUserWaits;
					int j;
					for (j = 0; j < numUserWaits2 && registeredWaitHandle != _registeredWaits[j]; j++)
					{
					}
					registeredWaitHandle.OnRemoveWait();
					if (j + 1 < numUserWaits2)
					{
						int num = j;
						int num2 = numUserWaits2;
						Array.Copy(_registeredWaits, num + 1, _registeredWaits, num, num2 - (num + 1));
						_registeredWaits[num2 - 1] = null;
						num++;
						num2++;
						Array.Copy(_waitHandles, num + 1, _waitHandles, num, num2 - (num + 1));
						_waitHandles[num2 - 1] = null;
					}
					else
					{
						_registeredWaits[j] = null;
						_waitHandles[j + 1] = null;
					}
					_numUserWaits = numUserWaits2 - 1;
					_pendingRemoves[i] = null;
					registeredWaitHandle.Handle.DangerousRelease();
				}
				_numPendingRemoves = 0;
				return _numUserWaits;
			}
			finally
			{
				threadPoolInstance._waitThreadLock.Release();
			}
		}

		private void QueueWaitCompletion(RegisteredWaitHandle registeredHandle, bool timedOut)
		{
			registeredHandle.RequestCallback();
			if (registeredHandle.Repeating)
			{
				if (!registeredHandle.IsInfiniteTimeout)
				{
					registeredHandle.RestartTimeout();
				}
			}
			else
			{
				UnregisterWait(registeredHandle, blocking: false);
			}
			ThreadPool.UnsafeQueueHighPriorityWorkItemInternal(new CompleteWaitThreadPoolWorkItem(registeredHandle, timedOut));
		}

		public bool RegisterWaitHandle(RegisteredWaitHandle handle)
		{
			if (_numUserWaits == 63)
			{
				return false;
			}
			bool success = false;
			handle.Handle.DangerousAddRef(ref success);
			_registeredWaits[_numUserWaits] = handle;
			_waitHandles[_numUserWaits + 1] = handle.Handle;
			_numUserWaits++;
			handle.WaitThread = this;
			_changeHandlesEvent.Set();
			return true;
		}

		public void UnregisterWait(RegisteredWaitHandle handle)
		{
			UnregisterWait(handle, blocking: true);
		}

		private void UnregisterWait(RegisteredWaitHandle handle, bool blocking)
		{
			bool flag = false;
			PortableThreadPool threadPoolInstance = ThreadPoolInstance;
			threadPoolInstance._waitThreadLock.Acquire();
			try
			{
				if (Array.IndexOf(_registeredWaits, handle) >= 0)
				{
					if (Array.IndexOf(_pendingRemoves, handle) < 0)
					{
						_pendingRemoves[_numPendingRemoves++] = handle;
						_changeHandlesEvent.Set();
					}
					flag = true;
				}
			}
			finally
			{
				threadPoolInstance._waitThreadLock.Release();
			}
			if (blocking)
			{
				if (handle.IsBlocking)
				{
					handle.WaitForCallbacks();
				}
				else if (flag)
				{
					handle.WaitForRemoval();
				}
			}
		}
	}

	private static class WorkerThread
	{
		private static readonly short ThreadsToKeepAlive = DetermineThreadsToKeepAlive();

		private static readonly LowLevelLifoSemaphore s_semaphore = new LowLevelLifoSemaphore(0, 32767, AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.UnfairSemaphoreSpinLimit", 70, allowNegative: false), delegate
		{
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadWait((uint)ThreadPoolInstance._separated.counts.VolatileRead().NumExistingThreads, 0u, 0);
			}
		});

		private static readonly ThreadStart s_workerThreadStart = WorkerThreadStart;

		private static bool IsIOPending
		{
			get
			{
				if (Interop.Kernel32.GetThreadIOPendingFlag(Interop.Kernel32.GetCurrentThread(), out var lpIOIsPending))
				{
					return lpIOIsPending != Interop.BOOL.FALSE;
				}
				return true;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WorkerDoWork(PortableThreadPool threadPoolInstance, ref bool spinWait)
		{
			bool flag = false;
			while (TakeActiveRequest(threadPoolInstance))
			{
				threadPoolInstance._separated.lastDequeueTime = Environment.TickCount;
				if (!ThreadPoolWorkQueue.Dispatch())
				{
					flag = true;
					break;
				}
				if (threadPoolInstance._separated.numRequestedWorkers <= 0)
				{
					break;
				}
				Thread.UninterruptibleSleep0();
				if (!Environment.IsSingleProcessor)
				{
					Thread.SpinWait(1);
				}
			}
			spinWait = !flag;
			if (!flag)
			{
				RemoveWorkingWorker(threadPoolInstance);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ShouldExitWorker(PortableThreadPool threadPoolInstance, LowLevelLock threadAdjustmentLock)
		{
			if (IsIOPending)
			{
				return false;
			}
			threadAdjustmentLock.Acquire();
			try
			{
				ThreadCounts threadCounts = threadPoolInstance._separated.counts;
				short num;
				short newThreadCount;
				while (true)
				{
					if (threadCounts.NumExistingThreads <= threadCounts.NumProcessingWork)
					{
						return false;
					}
					ThreadCounts newCounts = threadCounts;
					num = --newCounts.NumExistingThreads;
					newThreadCount = (newCounts.NumThreadsGoal = Math.Max(threadPoolInstance.MinThreadsGoal, Math.Min(num, threadCounts.NumThreadsGoal)));
					ThreadCounts threadCounts2 = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, threadCounts);
					if (threadCounts2 == threadCounts)
					{
						break;
					}
					threadCounts = threadCounts2;
				}
				HillClimbing.ThreadPoolHillClimber.ForceChange(newThreadCount, HillClimbing.StateOrTransition.ThreadTimedOut);
				if (NativeRuntimeEventSource.Log.IsEnabled())
				{
					NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadStop((uint)num, 0u, 0);
				}
				return true;
			}
			finally
			{
				threadAdjustmentLock.Release();
			}
		}

		private static void RemoveWorkingWorker(PortableThreadPool threadPoolInstance)
		{
			ThreadCounts threadCounts = threadPoolInstance._separated.counts;
			while (true)
			{
				ThreadCounts newCounts = threadCounts;
				newCounts.NumProcessingWork--;
				ThreadCounts threadCounts2 = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, threadCounts);
				if (threadCounts2 == threadCounts)
				{
					break;
				}
				threadCounts = threadCounts2;
			}
			if (threadPoolInstance._separated.numRequestedWorkers > 0)
			{
				MaybeAddWorkingWorker(threadPoolInstance);
			}
		}

		internal static void MaybeAddWorkingWorker(PortableThreadPool threadPoolInstance)
		{
			ThreadCounts threadCounts = threadPoolInstance._separated.counts;
			short numProcessingWork;
			short num;
			short numExistingThreads;
			short num2;
			while (true)
			{
				numProcessingWork = threadCounts.NumProcessingWork;
				if (numProcessingWork >= threadCounts.NumThreadsGoal)
				{
					return;
				}
				num = (short)(numProcessingWork + 1);
				numExistingThreads = threadCounts.NumExistingThreads;
				num2 = Math.Max(numExistingThreads, num);
				ThreadCounts newCounts = threadCounts;
				newCounts.NumProcessingWork = num;
				newCounts.NumExistingThreads = num2;
				ThreadCounts threadCounts2 = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, threadCounts);
				if (threadCounts2 == threadCounts)
				{
					break;
				}
				threadCounts = threadCounts2;
			}
			int num3 = num2 - numExistingThreads;
			int num4 = num - numProcessingWork;
			if (num4 > 0)
			{
				s_semaphore.Release(num4);
			}
			while (num3 > 0)
			{
				CreateWorkerThread();
				num3--;
			}
		}

		internal static bool ShouldStopProcessingWorkNow(PortableThreadPool threadPoolInstance)
		{
			ThreadCounts threadCounts = threadPoolInstance._separated.counts;
			while (true)
			{
				if (threadCounts.NumProcessingWork <= threadCounts.NumThreadsGoal)
				{
					return false;
				}
				ThreadCounts newCounts = threadCounts;
				newCounts.NumProcessingWork--;
				ThreadCounts threadCounts2 = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, threadCounts);
				if (threadCounts2 == threadCounts)
				{
					break;
				}
				threadCounts = threadCounts2;
			}
			return true;
		}

		private static bool TakeActiveRequest(PortableThreadPool threadPoolInstance)
		{
			int num = threadPoolInstance._separated.numRequestedWorkers;
			while (num > 0)
			{
				int num2 = Interlocked.CompareExchange(ref threadPoolInstance._separated.numRequestedWorkers, num - 1, num);
				if (num2 == num)
				{
					return true;
				}
				num = num2;
			}
			return false;
		}

		private static short DetermineThreadsToKeepAlive()
		{
			short int16Config = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.ThreadsToKeepAlive", "DOTNET_ThreadPool_ThreadsToKeepAlive", 0);
			if (int16Config < -1)
			{
				return 0;
			}
			return int16Config;
		}

		private static void WorkerThreadStart()
		{
			Thread.CurrentThread.SetThreadPoolWorkerThreadName();
			PortableThreadPool threadPoolInstance = ThreadPoolInstance;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadStart((uint)threadPoolInstance._separated.counts.VolatileRead().NumExistingThreads, 0u, 0);
			}
			LowLevelLock threadAdjustmentLock = threadPoolInstance._threadAdjustmentLock;
			LowLevelLifoSemaphore lowLevelLifoSemaphore = s_semaphore;
			int timeoutMs = ThreadPoolThreadTimeoutMs;
			if (ThreadsToKeepAlive != 0)
			{
				if (ThreadsToKeepAlive < 0)
				{
					timeoutMs = -1;
				}
				else
				{
					int num = threadPoolInstance._numThreadsBeingKeptAlive;
					while (num < ThreadsToKeepAlive)
					{
						int num2 = Interlocked.CompareExchange(ref threadPoolInstance._numThreadsBeingKeptAlive, num + 1, num);
						if (num2 == num)
						{
							timeoutMs = -1;
							break;
						}
						num = num2;
					}
				}
			}
			do
			{
				bool spinWait = true;
				while (lowLevelLifoSemaphore.Wait(timeoutMs, spinWait))
				{
					WorkerDoWork(threadPoolInstance, ref spinWait);
				}
			}
			while (!ShouldExitWorker(threadPoolInstance, threadAdjustmentLock));
		}

		private static void CreateWorkerThread()
		{
			Thread thread = new Thread(s_workerThreadStart);
			thread.IsThreadPoolThread = true;
			thread.IsBackground = true;
			thread.UnsafeStart();
		}
	}

	private struct CountsOfThreadsProcessingUserCallbacks : IEquatable<CountsOfThreadsProcessingUserCallbacks>
	{
		private uint _data;

		public short Current => GetInt16Value(0);

		public short HighWatermark => GetInt16Value(16);

		private CountsOfThreadsProcessingUserCallbacks(uint data)
		{
			_data = data;
		}

		private short GetInt16Value(byte shift)
		{
			return (short)(_data >> (int)shift);
		}

		private void SetInt16Value(short value, byte shift)
		{
			_data = (_data & (uint)(~(65535 << (int)shift))) | (uint)((ushort)value << (int)shift);
		}

		public void IncrementCurrent()
		{
			if (Current < HighWatermark)
			{
				_data++;
			}
			else
			{
				_data += 65537u;
			}
		}

		public void DecrementCurrent()
		{
			_data--;
		}

		public void ResetHighWatermark()
		{
			SetInt16Value(Current, 16);
		}

		public CountsOfThreadsProcessingUserCallbacks InterlockedCompareExchange(CountsOfThreadsProcessingUserCallbacks newCounts, CountsOfThreadsProcessingUserCallbacks oldCounts)
		{
			return new CountsOfThreadsProcessingUserCallbacks(Interlocked.CompareExchange(ref _data, newCounts._data, oldCounts._data));
		}

		public static bool operator ==(CountsOfThreadsProcessingUserCallbacks lhs, CountsOfThreadsProcessingUserCallbacks rhs)
		{
			return lhs.Equals(rhs);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is CountsOfThreadsProcessingUserCallbacks other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(CountsOfThreadsProcessingUserCallbacks other)
		{
			return _data == other._data;
		}

		public override int GetHashCode()
		{
			return (int)_data;
		}
	}

	private struct CpuUtilizationReader
	{
		public long _idleTime;

		public long _kernelTime;

		public long _userTime;

		public int CurrentUtilization
		{
			get
			{
				if (!Interop.Kernel32.GetSystemTimes(out var idle, out var kernel, out var user))
				{
					return 0;
				}
				long num = user - _userTime + (kernel - _kernelTime);
				long num2 = num - (idle - _idleTime);
				_kernelTime = kernel;
				_userTime = user;
				_idleTime = idle;
				if (num > 0 && num2 > 0)
				{
					long val = num2 * 100 / num;
					val = Math.Min(val, 100L);
					return (int)val;
				}
				return 0;
			}
		}
	}

	private static readonly bool s_initialized = ThreadPool.EnsureConfigInitialized();

	private static readonly short ForcedMinWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MinThreads", 0, allowNegative: false);

	private static readonly short ForcedMaxWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MaxThreads", 0, allowNegative: false);

	private static readonly int ThreadPoolThreadTimeoutMs = DetermineThreadPoolThreadTimeoutMs();

	[ThreadStatic]
	private static object t_completionCountObject;

	public static readonly PortableThreadPool ThreadPoolInstance = new PortableThreadPool();

	private int _cpuUtilization;

	private short _minThreads;

	private short _maxThreads;

	private short _legacy_minIOCompletionThreads;

	private short _legacy_maxIOCompletionThreads;

	private long _currentSampleStartTime;

	private readonly ThreadInt64PersistentCounter _completionCounter = new ThreadInt64PersistentCounter();

	private int _threadAdjustmentIntervalMs;

	private short _numBlockedThreads;

	private short _numThreadsAddedDueToBlocking;

	private PendingBlockingAdjustment _pendingBlockingAdjustment;

	private long _memoryUsageBytes;

	private long _memoryLimitBytes;

	private readonly nint _ioPort;

	private IOCompletionPoller[] _ioCompletionPollers;

	private readonly LowLevelLock _threadAdjustmentLock = new LowLevelLock();

	private CacheLineSeparated _separated;

	private static readonly bool UnsafeInlineIOCompletionCallbacks = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";

	private static readonly int IOCompletionPollerCount = GetIOCompletionPollerCount();

	private WaitThreadNode _waitThreadsHead;

	private readonly LowLevelLock _waitThreadLock = new LowLevelLock();

	private int _numThreadsBeingKeptAlive;

	private CountsOfThreadsProcessingUserCallbacks _countsOfThreadsProcessingUserCallbacks;

	private static bool HasForcedMinThreads
	{
		get
		{
			if (ForcedMinWorkerThreads > 0)
			{
				if (ForcedMaxWorkerThreads > 0)
				{
					return ForcedMinWorkerThreads <= ForcedMaxWorkerThreads;
				}
				return true;
			}
			return false;
		}
	}

	private static bool HasForcedMaxThreads
	{
		get
		{
			if (ForcedMaxWorkerThreads > 0)
			{
				if (ForcedMinWorkerThreads > 0)
				{
					return ForcedMinWorkerThreads <= ForcedMaxWorkerThreads;
				}
				return true;
			}
			return false;
		}
	}

	public int ThreadCount => _separated.counts.VolatileRead().NumExistingThreads;

	public long CompletedWorkItemCount => _completionCounter.Count;

	public short MinThreadsGoal => Math.Min(_separated.counts.NumThreadsGoal, TargetThreadsGoalForBlockingAdjustment);

	private short TargetThreadsGoalForBlockingAdjustment
	{
		get
		{
			if (_numBlockedThreads > 0)
			{
				return (short)Math.Min((ushort)(_minThreads + _numBlockedThreads), (ushort)_maxThreads);
			}
			return _minThreads;
		}
	}

	private static int DetermineThreadPoolThreadTimeoutMs()
	{
		int int32Config = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.ThreadTimeoutMs", "DOTNET_ThreadPool_ThreadTimeoutMs", 20000);
		if (int32Config < -1)
		{
			return 20000;
		}
		return int32Config;
	}

	private PortableThreadPool()
	{
		_minThreads = (HasForcedMinThreads ? ForcedMinWorkerThreads : ((short)Environment.ProcessorCount));
		if (_minThreads > short.MaxValue)
		{
			_minThreads = short.MaxValue;
		}
		_maxThreads = (HasForcedMaxThreads ? ForcedMaxWorkerThreads : short.MaxValue);
		if (_maxThreads > short.MaxValue)
		{
			_maxThreads = short.MaxValue;
		}
		else if (_maxThreads < _minThreads)
		{
			_maxThreads = _minThreads;
		}
		_legacy_minIOCompletionThreads = 1;
		_legacy_maxIOCompletionThreads = 1000;
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads((ushort)_minThreads, (ushort)_maxThreads, (ushort)_legacy_minIOCompletionThreads, (ushort)_legacy_maxIOCompletionThreads, 0);
		}
		_separated.counts.NumThreadsGoal = _minThreads;
		_ioPort = CreateIOCompletionPort();
	}

	public bool SetMinThreads(int workerThreads, int ioCompletionThreads)
	{
		if (workerThreads < 0 || ioCompletionThreads < 0)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		_threadAdjustmentLock.Acquire();
		try
		{
			if (workerThreads > _maxThreads)
			{
				return false;
			}
			if (ioCompletionThreads > _legacy_maxIOCompletionThreads)
			{
				return false;
			}
			if (HasForcedMinThreads && workerThreads != ForcedMinWorkerThreads)
			{
				return false;
			}
			_legacy_minIOCompletionThreads = (short)Math.Max(1, ioCompletionThreads);
			short num = (short)Math.Max(1, workerThreads);
			if (num == _minThreads)
			{
				return true;
			}
			_minThreads = num;
			if (_numBlockedThreads > 0)
			{
				if (_pendingBlockingAdjustment != PendingBlockingAdjustment.Immediately)
				{
					_pendingBlockingAdjustment = PendingBlockingAdjustment.Immediately;
					flag2 = true;
				}
			}
			else if (_separated.counts.NumThreadsGoal < num)
			{
				_separated.counts.InterlockedSetNumThreadsGoal(num);
				if (_separated.numRequestedWorkers > 0)
				{
					flag = true;
				}
			}
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads((ushort)_minThreads, (ushort)_maxThreads, (ushort)_legacy_minIOCompletionThreads, (ushort)_legacy_maxIOCompletionThreads, 0);
			}
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
		if (flag)
		{
			WorkerThread.MaybeAddWorkingWorker(this);
		}
		else if (flag2)
		{
			GateThread.Wake(this);
		}
		return true;
	}

	public void GetMinThreads(out int workerThreads, out int ioCompletionThreads)
	{
		workerThreads = Volatile.Read(ref _minThreads);
		ioCompletionThreads = _legacy_minIOCompletionThreads;
	}

	public bool SetMaxThreads(int workerThreads, int ioCompletionThreads)
	{
		if (workerThreads <= 0 || ioCompletionThreads <= 0)
		{
			return false;
		}
		_threadAdjustmentLock.Acquire();
		try
		{
			if (workerThreads < _minThreads)
			{
				return false;
			}
			if (ioCompletionThreads < _legacy_minIOCompletionThreads)
			{
				return false;
			}
			if (HasForcedMaxThreads && workerThreads != ForcedMaxWorkerThreads)
			{
				return false;
			}
			_legacy_maxIOCompletionThreads = (short)Math.Min(ioCompletionThreads, 32767);
			short num = (short)Math.Min(workerThreads, 32767);
			if (num == _maxThreads)
			{
				return true;
			}
			_maxThreads = num;
			if (_separated.counts.NumThreadsGoal > num)
			{
				_separated.counts.InterlockedSetNumThreadsGoal(num);
			}
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads((ushort)_minThreads, (ushort)_maxThreads, (ushort)_legacy_minIOCompletionThreads, (ushort)_legacy_maxIOCompletionThreads, 0);
			}
			return true;
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
	}

	public void GetMaxThreads(out int workerThreads, out int ioCompletionThreads)
	{
		workerThreads = Volatile.Read(ref _maxThreads);
		ioCompletionThreads = _legacy_maxIOCompletionThreads;
	}

	public void GetAvailableThreads(out int workerThreads, out int ioCompletionThreads)
	{
		ThreadCounts threadCounts = _separated.counts.VolatileRead();
		workerThreads = Math.Max(0, _maxThreads - threadCounts.NumProcessingWork);
		ioCompletionThreads = _legacy_maxIOCompletionThreads;
	}

	public object GetOrCreateThreadLocalCompletionCountObject()
	{
		return t_completionCountObject ?? CreateThreadLocalCompletionCountObject();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private object CreateThreadLocalCompletionCountObject()
	{
		return t_completionCountObject = _completionCounter.CreateThreadLocalCountObject();
	}

	private void NotifyWorkItemProgress(object threadLocalCompletionCountObject, int currentTimeMs)
	{
		ThreadInt64PersistentCounter.Increment(threadLocalCompletionCountObject);
		_separated.lastDequeueTime = currentTimeMs;
		if (ShouldAdjustMaxWorkersActive(currentTimeMs))
		{
			AdjustMaxWorkersActive();
		}
	}

	internal void NotifyWorkItemProgress()
	{
		NotifyWorkItemProgress(GetOrCreateThreadLocalCompletionCountObject(), Environment.TickCount);
	}

	internal bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs)
	{
		NotifyWorkItemProgress(threadLocalCompletionCountObject, currentTimeMs);
		return !WorkerThread.ShouldStopProcessingWorkNow(this);
	}

	private void AdjustMaxWorkersActive()
	{
		LowLevelLock threadAdjustmentLock = _threadAdjustmentLock;
		if (!threadAdjustmentLock.TryAcquire())
		{
			return;
		}
		bool flag = false;
		try
		{
			ThreadCounts counts = _separated.counts;
			if (counts.NumProcessingWork > counts.NumThreadsGoal || _pendingBlockingAdjustment != 0)
			{
				return;
			}
			long timestamp = Stopwatch.GetTimestamp();
			double totalSeconds = Stopwatch.GetElapsedTime(_currentSampleStartTime, timestamp).TotalSeconds;
			if (totalSeconds * 1000.0 >= (double)(_threadAdjustmentIntervalMs / 2))
			{
				int tickCount = Environment.TickCount;
				int num = (int)_completionCounter.Count;
				int numCompletions = num - _separated.priorCompletionCount;
				short numThreadsGoal = counts.NumThreadsGoal;
				int num2;
				(num2, _threadAdjustmentIntervalMs) = HillClimbing.ThreadPoolHillClimber.Update(numThreadsGoal, totalSeconds, numCompletions);
				if (numThreadsGoal != (short)num2)
				{
					_separated.counts.InterlockedSetNumThreadsGoal((short)num2);
					if (num2 > numThreadsGoal)
					{
						flag = true;
					}
				}
				_separated.priorCompletionCount = num;
				_separated.nextCompletedWorkRequestsTime = tickCount + _threadAdjustmentIntervalMs;
				Volatile.Write(ref _separated.priorCompletedWorkRequestsTime, tickCount);
				_currentSampleStartTime = timestamp;
			}
		}
		finally
		{
			threadAdjustmentLock.Release();
		}
		if (flag)
		{
			WorkerThread.MaybeAddWorkingWorker(this);
		}
	}

	private bool ShouldAdjustMaxWorkersActive(int currentTimeMs)
	{
		if (HillClimbing.IsDisabled)
		{
			return false;
		}
		int num = Volatile.Read(ref _separated.priorCompletedWorkRequestsTime);
		uint num2 = (uint)(_separated.nextCompletedWorkRequestsTime - num);
		uint num3 = (uint)(currentTimeMs - num);
		if (num3 < num2)
		{
			return false;
		}
		ThreadCounts counts = _separated.counts;
		if (counts.NumProcessingWork > counts.NumThreadsGoal)
		{
			return false;
		}
		return _pendingBlockingAdjustment == PendingBlockingAdjustment.None;
	}

	internal void RequestWorker()
	{
		Interlocked.Increment(ref _separated.numRequestedWorkers);
		WorkerThread.MaybeAddWorkingWorker(this);
		GateThread.EnsureRunning(this);
	}

	private bool OnGen2GCCallback()
	{
		GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
		_memoryLimitBytes = gCMemoryInfo.HighMemoryLoadThresholdBytes;
		_memoryUsageBytes = Math.Min(gCMemoryInfo.MemoryLoadBytes, gCMemoryInfo.HighMemoryLoadThresholdBytes);
		return true;
	}

	internal static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, bool flowExecutionContext)
	{
		ArgumentNullException.ThrowIfNull(waitObject, "waitObject");
		ArgumentNullException.ThrowIfNull(callBack, "callBack");
		RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(waitObject, new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext), (int)millisecondsTimeOutInterval, !executeOnlyOnce);
		ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);
		return registeredWaitHandle;
	}

	public bool NotifyThreadBlocked()
	{
		if (!BlockingConfig.IsCooperativeBlockingEnabled || !Thread.CurrentThread.IsThreadPoolThread)
		{
			return false;
		}
		bool flag = false;
		_threadAdjustmentLock.Acquire();
		try
		{
			_numBlockedThreads++;
			if (_pendingBlockingAdjustment != PendingBlockingAdjustment.WithDelayIfNecessary && _separated.counts.NumThreadsGoal < TargetThreadsGoalForBlockingAdjustment)
			{
				if (_pendingBlockingAdjustment == PendingBlockingAdjustment.None)
				{
					flag = true;
				}
				_pendingBlockingAdjustment = PendingBlockingAdjustment.WithDelayIfNecessary;
			}
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
		if (flag)
		{
			GateThread.Wake(this);
		}
		return true;
	}

	public void NotifyThreadUnblocked()
	{
		bool flag = false;
		_threadAdjustmentLock.Acquire();
		try
		{
			_numBlockedThreads--;
			if (_pendingBlockingAdjustment != PendingBlockingAdjustment.Immediately && _numThreadsAddedDueToBlocking > 0 && _separated.counts.NumThreadsGoal > TargetThreadsGoalForBlockingAdjustment)
			{
				flag = true;
				_pendingBlockingAdjustment = PendingBlockingAdjustment.Immediately;
			}
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
		if (flag)
		{
			GateThread.Wake(this);
		}
	}

	private uint PerformBlockingAdjustment(bool previousDelayElapsed)
	{
		_threadAdjustmentLock.Acquire();
		uint result;
		bool addWorker;
		try
		{
			result = PerformBlockingAdjustment(previousDelayElapsed, out addWorker);
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
		if (addWorker)
		{
			WorkerThread.MaybeAddWorkingWorker(this);
		}
		return result;
	}

	private uint PerformBlockingAdjustment(bool previousDelayElapsed, out bool addWorker)
	{
		_pendingBlockingAdjustment = PendingBlockingAdjustment.None;
		addWorker = false;
		short targetThreadsGoalForBlockingAdjustment = TargetThreadsGoalForBlockingAdjustment;
		ThreadCounts counts = _separated.counts;
		short num = counts.NumThreadsGoal;
		if (num == targetThreadsGoalForBlockingAdjustment)
		{
			return 0u;
		}
		if (num > targetThreadsGoalForBlockingAdjustment)
		{
			if (_numThreadsAddedDueToBlocking <= 0)
			{
				return 0u;
			}
			short num2 = Math.Min((short)(num - targetThreadsGoalForBlockingAdjustment), _numThreadsAddedDueToBlocking);
			_numThreadsAddedDueToBlocking -= num2;
			num -= num2;
			_separated.counts.InterlockedSetNumThreadsGoal(num);
			HillClimbing.ThreadPoolHillClimber.ForceChange(num, HillClimbing.StateOrTransition.CooperativeBlocking);
			return 0u;
		}
		short num3 = (short)Math.Min((ushort)(_minThreads + BlockingConfig.ThreadsToAddWithoutDelay), (ushort)_maxThreads);
		short val = Math.Max(num3, Math.Min(counts.NumExistingThreads, _maxThreads));
		short num4 = Math.Min(targetThreadsGoalForBlockingAdjustment, val);
		short num5;
		if (num < num4)
		{
			num5 = num4;
		}
		else
		{
			if (!previousDelayElapsed)
			{
				goto IL_01a6;
			}
			num5 = (short)(num + 1);
		}
		if (num5 > counts.NumExistingThreads && !BlockingConfig.IgnoreMemoryUsage)
		{
			long memoryLimitBytes = _memoryLimitBytes;
			if (memoryLimitBytes > 0)
			{
				long num6 = _memoryUsageBytes + (long)counts.NumExistingThreads * 65536L;
				long num7 = memoryLimitBytes * 8 / 10;
				if (num6 >= num7)
				{
					return 0u;
				}
				long val2 = counts.NumExistingThreads + (num7 - num6) / 65536;
				num5 = (short)Math.Min(num5, val2);
				if (num5 <= num)
				{
					return 0u;
				}
			}
		}
		_numThreadsAddedDueToBlocking += (short)(num5 - num);
		counts = _separated.counts.InterlockedSetNumThreadsGoal(num5);
		HillClimbing.ThreadPoolHillClimber.ForceChange(num5, HillClimbing.StateOrTransition.CooperativeBlocking);
		if (counts.NumProcessingWork >= num && _separated.numRequestedWorkers > 0)
		{
			addWorker = true;
		}
		num = num5;
		if (num >= targetThreadsGoalForBlockingAdjustment)
		{
			return 0u;
		}
		goto IL_01a6;
		IL_01a6:
		_pendingBlockingAdjustment = PendingBlockingAdjustment.WithDelayIfNecessary;
		int num8 = 1 + (num - num3) / BlockingConfig.ThreadsPerDelayStep;
		return Math.Min((uint)num8 * BlockingConfig.DelayStepMs, BlockingConfig.MaxDelayMs);
	}

	private static int GetIOCompletionPollerCount()
	{
		if (uint.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT"), out var result))
		{
			return Math.Min((int)result, 32767);
		}
		if (UnsafeInlineIOCompletionCallbacks)
		{
			return Environment.ProcessorCount;
		}
		int int32Config = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.ProcessorsPerIOPollerThread", 12, allowNegative: false);
		return (Environment.ProcessorCount - 1) / int32Config + 1;
	}

	private static nint CreateIOCompletionPort()
	{
		nint num = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, IOCompletionPollerCount);
		if (num == 0)
		{
			int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
			Environment.FailFast($"Failed to create an IO completion port. HR: {hRForLastWin32Error}");
		}
		return num;
	}

	public void RegisterForIOCompletionNotifications(nint handle)
	{
		if (_ioCompletionPollers == null)
		{
			EnsureIOCompletionPollers();
		}
		nint num = Interop.Kernel32.CreateIoCompletionPort(handle, _ioPort, UIntPtr.Zero, 0);
		if (num == 0)
		{
			ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
		}
	}

	public unsafe void QueueNativeOverlapped(NativeOverlapped* nativeOverlapped)
	{
		if (_ioCompletionPollers == null)
		{
			EnsureIOCompletionPollers();
		}
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(nativeOverlapped);
		}
		if (!Interop.Kernel32.PostQueuedCompletionStatus(_ioPort, 0u, UIntPtr.Zero, (nint)nativeOverlapped))
		{
			ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void EnsureIOCompletionPollers()
	{
		_threadAdjustmentLock.Acquire();
		try
		{
			if (_ioCompletionPollers == null)
			{
				IOCompletionPoller[] array = new IOCompletionPoller[IOCompletionPollerCount];
				for (int i = 0; i < IOCompletionPollerCount; i++)
				{
					array[i] = new IOCompletionPoller(_ioPort);
				}
				_ioCompletionPollers = array;
			}
		}
		catch (Exception exception)
		{
			Environment.FailFast("Failed to initialize IO completion pollers.", exception);
		}
		finally
		{
			_threadAdjustmentLock.Release();
		}
	}

	internal void RegisterWaitHandle(RegisteredWaitHandle handle)
	{
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(handle);
		}
		_waitThreadLock.Acquire();
		try
		{
			WaitThreadNode waitThreadNode = _waitThreadsHead ?? (_waitThreadsHead = new WaitThreadNode(new WaitThread()));
			WaitThreadNode waitThreadNode2;
			do
			{
				if (waitThreadNode.Thread.RegisterWaitHandle(handle))
				{
					return;
				}
				waitThreadNode2 = waitThreadNode;
				waitThreadNode = waitThreadNode.Next;
			}
			while (waitThreadNode != null);
			waitThreadNode2.Next = new WaitThreadNode(new WaitThread());
			waitThreadNode2.Next.Thread.RegisterWaitHandle(handle);
		}
		finally
		{
			_waitThreadLock.Release();
		}
	}

	internal static void CompleteWait(RegisteredWaitHandle handle, bool timedOut)
	{
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIODequeue(handle);
		}
		handle.PerformCallback(timedOut);
	}

	private bool TryRemoveWaitThread(WaitThread thread)
	{
		_waitThreadLock.Acquire();
		try
		{
			if (thread.AnyUserWaits)
			{
				return false;
			}
			RemoveWaitThread(thread);
		}
		finally
		{
			_waitThreadLock.Release();
		}
		return true;
	}

	private void RemoveWaitThread(WaitThread thread)
	{
		WaitThreadNode waitThreadNode = _waitThreadsHead;
		if (waitThreadNode.Thread == thread)
		{
			_waitThreadsHead = waitThreadNode.Next;
			return;
		}
		WaitThreadNode waitThreadNode2;
		do
		{
			waitThreadNode2 = waitThreadNode;
			waitThreadNode = waitThreadNode.Next;
		}
		while (waitThreadNode != null && waitThreadNode.Thread != thread);
		if (waitThreadNode != null)
		{
			waitThreadNode2.Next = waitThreadNode.Next;
		}
	}

	public void ReportThreadStatus(bool isProcessingUserCallback)
	{
		CountsOfThreadsProcessingUserCallbacks countsOfThreadsProcessingUserCallbacks = _countsOfThreadsProcessingUserCallbacks;
		while (true)
		{
			CountsOfThreadsProcessingUserCallbacks newCounts = countsOfThreadsProcessingUserCallbacks;
			if (isProcessingUserCallback)
			{
				newCounts.IncrementCurrent();
			}
			else
			{
				newCounts.DecrementCurrent();
			}
			CountsOfThreadsProcessingUserCallbacks countsOfThreadsProcessingUserCallbacks2 = _countsOfThreadsProcessingUserCallbacks.InterlockedCompareExchange(newCounts, countsOfThreadsProcessingUserCallbacks);
			if (!(countsOfThreadsProcessingUserCallbacks2 == countsOfThreadsProcessingUserCallbacks))
			{
				countsOfThreadsProcessingUserCallbacks = countsOfThreadsProcessingUserCallbacks2;
				continue;
			}
			break;
		}
	}

	private short GetAndResetHighWatermarkCountOfThreadsProcessingUserCallbacks()
	{
		CountsOfThreadsProcessingUserCallbacks countsOfThreadsProcessingUserCallbacks = _countsOfThreadsProcessingUserCallbacks;
		CountsOfThreadsProcessingUserCallbacks countsOfThreadsProcessingUserCallbacks2;
		while (true)
		{
			CountsOfThreadsProcessingUserCallbacks newCounts = countsOfThreadsProcessingUserCallbacks;
			newCounts.ResetHighWatermark();
			countsOfThreadsProcessingUserCallbacks2 = _countsOfThreadsProcessingUserCallbacks.InterlockedCompareExchange(newCounts, countsOfThreadsProcessingUserCallbacks);
			if (countsOfThreadsProcessingUserCallbacks2 == countsOfThreadsProcessingUserCallbacks || countsOfThreadsProcessingUserCallbacks2.HighWatermark == countsOfThreadsProcessingUserCallbacks2.Current)
			{
				break;
			}
			countsOfThreadsProcessingUserCallbacks = countsOfThreadsProcessingUserCallbacks2;
		}
		return countsOfThreadsProcessingUserCallbacks2.HighWatermark;
	}
}

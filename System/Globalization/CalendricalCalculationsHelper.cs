using System.Runtime.CompilerServices;

namespace System.Globalization;

internal static class CalendricalCalculationsHelper
{
	private enum CorrectionAlgorithm
	{
		Default,
		Year1988to2019,
		Year1900to1987,
		Year1800to1899,
		Year1700to1799,
		Year1620to1699
	}

	private readonly struct EphemerisCorrectionAlgorithmMap
	{
		internal readonly int _lowestYear;

		internal readonly CorrectionAlgorithm _algorithm;

		public EphemerisCorrectionAlgorithmMap(int year, CorrectionAlgorithm algorithm)
		{
			_lowestYear = year;
			_algorithm = algorithm;
		}
	}

	private static readonly long s_startOf1810 = GetNumberOfDays(new DateTime(1810, 1, 1));

	private static readonly long s_startOf1900Century = GetNumberOfDays(new DateTime(1900, 1, 1));

	private static readonly EphemerisCorrectionAlgorithmMap[] s_ephemerisCorrectionTable = new EphemerisCorrectionAlgorithmMap[7]
	{
		new EphemerisCorrectionAlgorithmMap(2020, CorrectionAlgorithm.Default),
		new EphemerisCorrectionAlgorithmMap(1988, CorrectionAlgorithm.Year1988to2019),
		new EphemerisCorrectionAlgorithmMap(1900, CorrectionAlgorithm.Year1900to1987),
		new EphemerisCorrectionAlgorithmMap(1800, CorrectionAlgorithm.Year1800to1899),
		new EphemerisCorrectionAlgorithmMap(1700, CorrectionAlgorithm.Year1700to1799),
		new EphemerisCorrectionAlgorithmMap(1620, CorrectionAlgorithm.Year1620to1699),
		new EphemerisCorrectionAlgorithmMap(int.MinValue, CorrectionAlgorithm.Default)
	};

	private static ReadOnlySpan<double> Coefficients1900to1987 => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> Coefficients1800to1899 => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> Coefficients1700to1799 => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> Coefficients1620to1699 => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> LambdaCoefficients => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> AnomalyCoefficients => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> EccentricityCoefficients => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> CoefficientsA => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> CoefficientsB => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<double> Coefficients => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static double RadiansFromDegrees(double degree)
	{
		return degree * Math.PI / 180.0;
	}

	private static double SinOfDegree(double degree)
	{
		return Math.Sin(RadiansFromDegrees(degree));
	}

	private static double CosOfDegree(double degree)
	{
		return Math.Cos(RadiansFromDegrees(degree));
	}

	private static double TanOfDegree(double degree)
	{
		return Math.Tan(RadiansFromDegrees(degree));
	}

	private static double Obliquity(double julianCenturies)
	{
		return PolynomialSum(Coefficients, julianCenturies);
	}

	internal static long GetNumberOfDays(DateTime date)
	{
		return date.Ticks / 864000000000L;
	}

	private static int GetGregorianYear(double numberOfDays)
	{
		return new DateTime(Math.Min((long)(Math.Floor(numberOfDays) * 864000000000.0), DateTime.MaxValue.Ticks)).Year;
	}

	private static double Reminder(double divisor, double dividend)
	{
		double num = Math.Floor(divisor / dividend);
		return divisor - dividend * num;
	}

	private static double NormalizeLongitude(double longitude)
	{
		longitude = Reminder(longitude, 360.0);
		if (longitude < 0.0)
		{
			longitude += 360.0;
		}
		return longitude;
	}

	public static double AsDayFraction(double longitude)
	{
		return longitude / 360.0;
	}

	private static double PolynomialSum(ReadOnlySpan<double> coefficients, double indeterminate)
	{
		double num = coefficients[0];
		double num2 = 1.0;
		for (int i = 1; i < coefficients.Length; i++)
		{
			num2 *= indeterminate;
			num += coefficients[i] * num2;
		}
		return num;
	}

	private static double CenturiesFrom1900(int gregorianYear)
	{
		long numberOfDays = GetNumberOfDays(new DateTime(gregorianYear, 7, 1));
		return (double)(numberOfDays - s_startOf1900Century) / 36525.0;
	}

	private static double DefaultEphemerisCorrection(int gregorianYear)
	{
		long numberOfDays = GetNumberOfDays(new DateTime(gregorianYear, 1, 1));
		double num = numberOfDays - s_startOf1810;
		double x = 0.5 + num;
		return (Math.Pow(x, 2.0) / 41048480.0 - 15.0) / 86400.0;
	}

	private static double EphemerisCorrection1988to2019(int gregorianYear)
	{
		return (double)(gregorianYear - 1933) / 86400.0;
	}

	private static double EphemerisCorrection1900to1987(int gregorianYear)
	{
		double indeterminate = CenturiesFrom1900(gregorianYear);
		return PolynomialSum(Coefficients1900to1987, indeterminate);
	}

	private static double EphemerisCorrection1800to1899(int gregorianYear)
	{
		double indeterminate = CenturiesFrom1900(gregorianYear);
		return PolynomialSum(Coefficients1800to1899, indeterminate);
	}

	private static double EphemerisCorrection1700to1799(int gregorianYear)
	{
		double indeterminate = gregorianYear - 1700;
		return PolynomialSum(Coefficients1700to1799, indeterminate) / 86400.0;
	}

	private static double EphemerisCorrection1620to1699(int gregorianYear)
	{
		double indeterminate = gregorianYear - 1600;
		return PolynomialSum(Coefficients1620to1699, indeterminate) / 86400.0;
	}

	private static double EphemerisCorrection(double time)
	{
		int gregorianYear = GetGregorianYear(time);
		EphemerisCorrectionAlgorithmMap[] array = s_ephemerisCorrectionTable;
		for (int i = 0; i < array.Length; i++)
		{
			EphemerisCorrectionAlgorithmMap ephemerisCorrectionAlgorithmMap = array[i];
			if (ephemerisCorrectionAlgorithmMap._lowestYear <= gregorianYear)
			{
				switch (ephemerisCorrectionAlgorithmMap._algorithm)
				{
				case CorrectionAlgorithm.Default:
					return DefaultEphemerisCorrection(gregorianYear);
				case CorrectionAlgorithm.Year1988to2019:
					return EphemerisCorrection1988to2019(gregorianYear);
				case CorrectionAlgorithm.Year1900to1987:
					return EphemerisCorrection1900to1987(gregorianYear);
				case CorrectionAlgorithm.Year1800to1899:
					return EphemerisCorrection1800to1899(gregorianYear);
				case CorrectionAlgorithm.Year1700to1799:
					return EphemerisCorrection1700to1799(gregorianYear);
				case CorrectionAlgorithm.Year1620to1699:
					return EphemerisCorrection1620to1699(gregorianYear);
				}
				break;
			}
		}
		return DefaultEphemerisCorrection(gregorianYear);
	}

	public static double JulianCenturies(double moment)
	{
		double num = moment + EphemerisCorrection(moment);
		return (num - 730120.5) / 36525.0;
	}

	private static double EquationOfTime(double time)
	{
		double num = JulianCenturies(time);
		double num2 = PolynomialSum(LambdaCoefficients, num);
		double num3 = PolynomialSum(AnomalyCoefficients, num);
		double num4 = PolynomialSum(EccentricityCoefficients, num);
		double num5 = Obliquity(num);
		double num6 = TanOfDegree(num5 / 2.0);
		double num7 = num6 * num6;
		double num8 = num7 * SinOfDegree(2.0 * num2) - 2.0 * num4 * SinOfDegree(num3) + 4.0 * num4 * num7 * SinOfDegree(num3) * CosOfDegree(2.0 * num2) - 0.5 * Math.Pow(num7, 2.0) * SinOfDegree(4.0 * num2) - 1.25 * Math.Pow(num4, 2.0) * SinOfDegree(2.0 * num3);
		double num9 = num8 / (Math.PI * 2.0);
		return Math.CopySign(Math.Min(Math.Abs(num9), 0.5), num9);
	}

	private static double AsLocalTime(double apparentMidday, double longitude)
	{
		double time = apparentMidday - AsDayFraction(longitude);
		return apparentMidday - EquationOfTime(time);
	}

	public static double Midday(double date, double longitude)
	{
		return AsLocalTime(date + 0.5, longitude) - AsDayFraction(longitude);
	}

	private static double InitLongitude(double longitude)
	{
		return NormalizeLongitude(longitude + 180.0) - 180.0;
	}

	public static double MiddayAtPersianObservationSite(double date)
	{
		return Midday(date, InitLongitude(52.5));
	}

	private static double PeriodicTerm(double julianCenturies, int x, double y, double z)
	{
		return (double)x * SinOfDegree(y + z * julianCenturies);
	}

	private static double SumLongSequenceOfPeriodicTerms(double julianCenturies)
	{
		double num = 0.0;
		num += PeriodicTerm(julianCenturies, 403406, 270.54861, 0.9287892);
		num += PeriodicTerm(julianCenturies, 195207, 340.19128, 35999.1376958);
		num += PeriodicTerm(julianCenturies, 119433, 63.91854, 35999.4089666);
		num += PeriodicTerm(julianCenturies, 112392, 331.2622, 35998.7287385);
		num += PeriodicTerm(julianCenturies, 3891, 317.843, 71998.20261);
		num += PeriodicTerm(julianCenturies, 2819, 86.631, 71998.4403);
		num += PeriodicTerm(julianCenturies, 1721, 240.052, 36000.35726);
		num += PeriodicTerm(julianCenturies, 660, 310.26, 71997.4812);
		num += PeriodicTerm(julianCenturies, 350, 247.23, 32964.4678);
		num += PeriodicTerm(julianCenturies, 334, 260.87, -19.441);
		num += PeriodicTerm(julianCenturies, 314, 297.82, 445267.1117);
		num += PeriodicTerm(julianCenturies, 268, 343.14, 45036.884);
		num += PeriodicTerm(julianCenturies, 242, 166.79, 3.1008);
		num += PeriodicTerm(julianCenturies, 234, 81.53, 22518.4434);
		num += PeriodicTerm(julianCenturies, 158, 3.5, -19.9739);
		num += PeriodicTerm(julianCenturies, 132, 132.75, 65928.9345);
		num += PeriodicTerm(julianCenturies, 129, 182.95, 9038.0293);
		num += PeriodicTerm(julianCenturies, 114, 162.03, 3034.7684);
		num += PeriodicTerm(julianCenturies, 99, 29.8, 33718.148);
		num += PeriodicTerm(julianCenturies, 93, 266.4, 3034.448);
		num += PeriodicTerm(julianCenturies, 86, 249.2, -2280.773);
		num += PeriodicTerm(julianCenturies, 78, 157.6, 29929.992);
		num += PeriodicTerm(julianCenturies, 72, 257.8, 31556.493);
		num += PeriodicTerm(julianCenturies, 68, 185.1, 149.588);
		num += PeriodicTerm(julianCenturies, 64, 69.9, 9037.75);
		num += PeriodicTerm(julianCenturies, 46, 8.0, 107997.405);
		num += PeriodicTerm(julianCenturies, 38, 197.1, -4444.176);
		num += PeriodicTerm(julianCenturies, 37, 250.4, 151.771);
		num += PeriodicTerm(julianCenturies, 32, 65.3, 67555.316);
		num += PeriodicTerm(julianCenturies, 29, 162.7, 31556.08);
		num += PeriodicTerm(julianCenturies, 28, 341.5, -4561.54);
		num += PeriodicTerm(julianCenturies, 27, 291.6, 107996.706);
		num += PeriodicTerm(julianCenturies, 27, 98.5, 1221.655);
		num += PeriodicTerm(julianCenturies, 25, 146.7, 62894.167);
		num += PeriodicTerm(julianCenturies, 24, 110.0, 31437.369);
		num += PeriodicTerm(julianCenturies, 21, 5.2, 14578.298);
		num += PeriodicTerm(julianCenturies, 21, 342.6, -31931.757);
		num += PeriodicTerm(julianCenturies, 20, 230.9, 34777.243);
		num += PeriodicTerm(julianCenturies, 18, 256.1, 1221.999);
		num += PeriodicTerm(julianCenturies, 17, 45.3, 62894.511);
		num += PeriodicTerm(julianCenturies, 14, 242.9, -4442.039);
		num += PeriodicTerm(julianCenturies, 13, 115.2, 107997.909);
		num += PeriodicTerm(julianCenturies, 13, 151.8, 119.066);
		num += PeriodicTerm(julianCenturies, 13, 285.3, 16859.071);
		num += PeriodicTerm(julianCenturies, 12, 53.3, -4.578);
		num += PeriodicTerm(julianCenturies, 10, 126.6, 26895.292);
		num += PeriodicTerm(julianCenturies, 10, 205.7, -39.127);
		num += PeriodicTerm(julianCenturies, 10, 85.9, 12297.536);
		return num + PeriodicTerm(julianCenturies, 10, 146.1, 90073.778);
	}

	private static double Aberration(double julianCenturies)
	{
		return 9.74E-05 * CosOfDegree(177.63 + 35999.01848 * julianCenturies) - 0.005575;
	}

	private static double Nutation(double julianCenturies)
	{
		double degree = PolynomialSum(CoefficientsA, julianCenturies);
		double degree2 = PolynomialSum(CoefficientsB, julianCenturies);
		return -0.004778 * SinOfDegree(degree) - 0.0003667 * SinOfDegree(degree2);
	}

	public static double Compute(double time)
	{
		double num = JulianCenturies(time);
		double num2 = 282.7771834 + 36000.76953744 * num + 5.729577951308232E-06 * SumLongSequenceOfPeriodicTerms(num);
		double longitude = num2 + Aberration(num) + Nutation(num);
		return InitLongitude(longitude);
	}

	public static double AsSeason(double longitude)
	{
		if (!(longitude < 0.0))
		{
			return longitude;
		}
		return longitude + 360.0;
	}

	private static double EstimatePrior(double longitude, double time)
	{
		double num = time - 1.0145616361111112 * AsSeason(InitLongitude(Compute(time) - longitude));
		double num2 = InitLongitude(Compute(num) - longitude);
		return Math.Min(time, num - 1.0145616361111112 * num2);
	}

	internal static long PersianNewYearOnOrBefore(long numberOfDays)
	{
		double date = numberOfDays;
		double d = EstimatePrior(0.0, MiddayAtPersianObservationSite(date));
		long num = (long)Math.Floor(d) - 1;
		long num2 = num + 3;
		long num3;
		for (num3 = num; num3 != num2; num3++)
		{
			double time = MiddayAtPersianObservationSite(num3);
			double num4 = Compute(time);
			if (0.0 <= num4 && num4 <= 2.0)
			{
				break;
			}
		}
		return num3 - 1;
	}
}

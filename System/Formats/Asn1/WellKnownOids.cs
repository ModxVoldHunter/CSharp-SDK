namespace System.Formats.Asn1;

internal static class WellKnownOids
{
	internal static string GetValue(ReadOnlySpan<byte> contents)
	{
		switch (contents.Length)
		{
		case 7:
		{
			byte b = contents[0];
			if (b != 42)
			{
				break;
			}
			byte b2 = contents[1];
			if (b2 != 134)
			{
				break;
			}
			byte b3 = contents[2];
			if (b3 != 72)
			{
				break;
			}
			byte b4 = contents[3];
			if (b4 != 206)
			{
				break;
			}
			switch (contents[4])
			{
			case 56:
			{
				byte b6 = contents[5];
				if (b6 == 4)
				{
					switch (contents[6])
					{
					case 1:
						return "1.2.840.10040.4.1";
					case 3:
						return "1.2.840.10040.4.3";
					}
				}
				break;
			}
			case 61:
				switch (contents[5])
				{
				case 2:
				{
					byte b8 = contents[6];
					if (b8 != 1)
					{
						break;
					}
					return "1.2.840.10045.2.1";
				}
				case 1:
					switch (contents[6])
					{
					case 1:
						return "1.2.840.10045.1.1";
					case 2:
						return "1.2.840.10045.1.2";
					}
					break;
				case 4:
				{
					byte b8 = contents[6];
					if (b8 != 1)
					{
						break;
					}
					return "1.2.840.10045.4.1";
				}
				}
				break;
			}
			break;
		}
		case 8:
			switch (contents[0])
			{
			case 42:
			{
				byte b2 = contents[1];
				if (b2 != 134)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 72)
				{
					break;
				}
				switch (contents[3])
				{
				case 206:
				{
					byte b5 = contents[4];
					if (b5 != 61)
					{
						break;
					}
					switch (contents[5])
					{
					case 3:
					{
						byte b8 = contents[6];
						if (b8 == 1)
						{
							byte b7 = contents[7];
							if (b7 == 7)
							{
								return "1.2.840.10045.3.1.7";
							}
						}
						break;
					}
					case 4:
					{
						byte b8 = contents[6];
						if (b8 == 3)
						{
							switch (contents[7])
							{
							case 2:
								return "1.2.840.10045.4.3.2";
							case 3:
								return "1.2.840.10045.4.3.3";
							case 4:
								return "1.2.840.10045.4.3.4";
							}
						}
						break;
					}
					}
					break;
				}
				case 134:
				{
					byte b5 = contents[4];
					if (b5 != 247)
					{
						break;
					}
					byte b6 = contents[5];
					if (b6 != 13)
					{
						break;
					}
					switch (contents[6])
					{
					case 2:
						switch (contents[7])
						{
						case 5:
							return "1.2.840.113549.2.5";
						case 7:
							return "1.2.840.113549.2.7";
						case 9:
							return "1.2.840.113549.2.9";
						case 10:
							return "1.2.840.113549.2.10";
						case 11:
							return "1.2.840.113549.2.11";
						}
						break;
					case 3:
						switch (contents[7])
						{
						case 2:
							return "1.2.840.113549.3.2";
						case 7:
							return "1.2.840.113549.3.7";
						}
						break;
					}
					break;
				}
				}
				break;
			}
			case 43:
			{
				byte b2 = contents[1];
				if (b2 != 6)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 1)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 != 5)
				{
					break;
				}
				byte b5 = contents[4];
				if (b5 != 5)
				{
					break;
				}
				byte b6 = contents[5];
				if (b6 != 7)
				{
					break;
				}
				switch (contents[6])
				{
				case 3:
					switch (contents[7])
					{
					case 1:
						return "1.3.6.1.5.5.7.3.1";
					case 2:
						return "1.3.6.1.5.5.7.3.2";
					case 3:
						return "1.3.6.1.5.5.7.3.3";
					case 4:
						return "1.3.6.1.5.5.7.3.4";
					case 8:
						return "1.3.6.1.5.5.7.3.8";
					case 9:
						return "1.3.6.1.5.5.7.3.9";
					}
					break;
				case 6:
				{
					byte b7 = contents[7];
					if (b7 != 2)
					{
						break;
					}
					return "1.3.6.1.5.5.7.6.2";
				}
				case 48:
					switch (contents[7])
					{
					case 1:
						return "1.3.6.1.5.5.7.48.1";
					case 2:
						return "1.3.6.1.5.5.7.48.2";
					}
					break;
				}
				break;
			}
			}
			break;
		case 9:
			switch (contents[0])
			{
			case 42:
			{
				byte b2 = contents[1];
				if (b2 != 134)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 72)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 != 134)
				{
					break;
				}
				byte b5 = contents[4];
				if (b5 != 247)
				{
					break;
				}
				byte b6 = contents[5];
				if (b6 != 13)
				{
					break;
				}
				byte b8 = contents[6];
				if (b8 != 1)
				{
					break;
				}
				switch (contents[7])
				{
				case 1:
					switch (contents[8])
					{
					case 1:
						return "1.2.840.113549.1.1.1";
					case 5:
						return "1.2.840.113549.1.1.5";
					case 7:
						return "1.2.840.113549.1.1.7";
					case 8:
						return "1.2.840.113549.1.1.8";
					case 9:
						return "1.2.840.113549.1.1.9";
					case 10:
						return "1.2.840.113549.1.1.10";
					case 11:
						return "1.2.840.113549.1.1.11";
					case 12:
						return "1.2.840.113549.1.1.12";
					case 13:
						return "1.2.840.113549.1.1.13";
					}
					break;
				case 5:
					switch (contents[8])
					{
					case 3:
						return "1.2.840.113549.1.5.3";
					case 10:
						return "1.2.840.113549.1.5.10";
					case 11:
						return "1.2.840.113549.1.5.11";
					case 12:
						return "1.2.840.113549.1.5.12";
					case 13:
						return "1.2.840.113549.1.5.13";
					}
					break;
				case 7:
					switch (contents[8])
					{
					case 1:
						return "1.2.840.113549.1.7.1";
					case 2:
						return "1.2.840.113549.1.7.2";
					case 3:
						return "1.2.840.113549.1.7.3";
					case 6:
						return "1.2.840.113549.1.7.6";
					}
					break;
				case 9:
					switch (contents[8])
					{
					case 1:
						return "1.2.840.113549.1.9.1";
					case 3:
						return "1.2.840.113549.1.9.3";
					case 4:
						return "1.2.840.113549.1.9.4";
					case 5:
						return "1.2.840.113549.1.9.5";
					case 6:
						return "1.2.840.113549.1.9.6";
					case 7:
						return "1.2.840.113549.1.9.7";
					case 14:
						return "1.2.840.113549.1.9.14";
					case 15:
						return "1.2.840.113549.1.9.15";
					case 20:
						return "1.2.840.113549.1.9.20";
					case 21:
						return "1.2.840.113549.1.9.21";
					}
					break;
				}
				break;
			}
			case 43:
			{
				byte b2 = contents[1];
				if (b2 != 6)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 1)
				{
					break;
				}
				switch (contents[3])
				{
				case 4:
				{
					byte b5 = contents[4];
					if (b5 != 1)
					{
						break;
					}
					byte b6 = contents[5];
					if (b6 != 130)
					{
						break;
					}
					byte b8 = contents[6];
					if (b8 != 55)
					{
						break;
					}
					byte b7 = contents[7];
					if (b7 == 17)
					{
						byte b9 = contents[8];
						if (b9 == 1)
						{
							return "1.3.6.1.4.1.311.17.1";
						}
					}
					break;
				}
				case 5:
				{
					byte b5 = contents[4];
					if (b5 != 5)
					{
						break;
					}
					byte b6 = contents[5];
					if (b6 != 7)
					{
						break;
					}
					byte b8 = contents[6];
					if (b8 != 48)
					{
						break;
					}
					byte b7 = contents[7];
					if (b7 == 1)
					{
						byte b9 = contents[8];
						if (b9 == 2)
						{
							return "1.3.6.1.5.5.7.48.1.2";
						}
					}
					break;
				}
				}
				break;
			}
			case 96:
			{
				byte b2 = contents[1];
				if (b2 != 134)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 72)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 != 1)
				{
					break;
				}
				byte b5 = contents[4];
				if (b5 != 101)
				{
					break;
				}
				byte b6 = contents[5];
				if (b6 != 3)
				{
					break;
				}
				byte b8 = contents[6];
				if (b8 != 4)
				{
					break;
				}
				switch (contents[7])
				{
				case 1:
					switch (contents[8])
					{
					case 2:
						return "2.16.840.1.101.3.4.1.2";
					case 22:
						return "2.16.840.1.101.3.4.1.22";
					case 42:
						return "2.16.840.1.101.3.4.1.42";
					}
					break;
				case 2:
					switch (contents[8])
					{
					case 1:
						return "2.16.840.1.101.3.4.2.1";
					case 2:
						return "2.16.840.1.101.3.4.2.2";
					case 3:
						return "2.16.840.1.101.3.4.2.3";
					}
					break;
				}
				break;
			}
			}
			break;
		case 11:
		{
			byte b = contents[0];
			if (b != 42)
			{
				break;
			}
			byte b2 = contents[1];
			if (b2 != 134)
			{
				break;
			}
			byte b3 = contents[2];
			if (b3 != 72)
			{
				break;
			}
			byte b4 = contents[3];
			if (b4 != 134)
			{
				break;
			}
			byte b5 = contents[4];
			if (b5 != 247)
			{
				break;
			}
			byte b6 = contents[5];
			if (b6 != 13)
			{
				break;
			}
			byte b8 = contents[6];
			if (b8 != 1)
			{
				break;
			}
			switch (contents[7])
			{
			case 9:
			{
				byte b9 = contents[8];
				if (b9 != 16)
				{
					break;
				}
				switch (contents[9])
				{
				case 1:
				{
					byte b11 = contents[10];
					if (b11 != 4)
					{
						break;
					}
					return "1.2.840.113549.1.9.16.1.4";
				}
				case 2:
					switch (contents[10])
					{
					case 12:
						return "1.2.840.113549.1.9.16.2.12";
					case 14:
						return "1.2.840.113549.1.9.16.2.14";
					case 47:
						return "1.2.840.113549.1.9.16.2.47";
					}
					break;
				}
				break;
			}
			case 12:
			{
				byte b9 = contents[8];
				if (b9 != 10)
				{
					break;
				}
				byte b10 = contents[9];
				if (b10 == 1)
				{
					switch (contents[10])
					{
					case 1:
						return "1.2.840.113549.1.12.10.1.1";
					case 2:
						return "1.2.840.113549.1.12.10.1.2";
					case 3:
						return "1.2.840.113549.1.12.10.1.3";
					case 5:
						return "1.2.840.113549.1.12.10.1.5";
					case 6:
						return "1.2.840.113549.1.12.10.1.6";
					}
				}
				break;
			}
			}
			break;
		}
		case 10:
			switch (contents[0])
			{
			case 42:
			{
				byte b2 = contents[1];
				if (b2 != 134)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 72)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 != 134)
				{
					break;
				}
				byte b5 = contents[4];
				if (b5 != 247)
				{
					break;
				}
				byte b6 = contents[5];
				if (b6 != 13)
				{
					break;
				}
				byte b8 = contents[6];
				if (b8 != 1)
				{
					break;
				}
				switch (contents[7])
				{
				case 9:
				{
					byte b9 = contents[8];
					if (b9 == 22)
					{
						byte b10 = contents[9];
						if (b10 == 1)
						{
							return "1.2.840.113549.1.9.22.1";
						}
					}
					break;
				}
				case 12:
				{
					byte b9 = contents[8];
					if (b9 == 1)
					{
						switch (contents[9])
						{
						case 3:
							return "1.2.840.113549.1.12.1.3";
						case 5:
							return "1.2.840.113549.1.12.1.5";
						case 6:
							return "1.2.840.113549.1.12.1.6";
						}
					}
					break;
				}
				}
				break;
			}
			case 43:
			{
				byte b2 = contents[1];
				if (b2 != 6)
				{
					break;
				}
				byte b3 = contents[2];
				if (b3 != 1)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 != 4)
				{
					break;
				}
				byte b5 = contents[4];
				if (b5 != 1)
				{
					break;
				}
				byte b6 = contents[5];
				if (b6 != 130)
				{
					break;
				}
				byte b8 = contents[6];
				if (b8 != 55)
				{
					break;
				}
				switch (contents[7])
				{
				case 17:
				{
					byte b9 = contents[8];
					if (b9 == 3)
					{
						byte b10 = contents[9];
						if (b10 == 20)
						{
							return "1.3.6.1.4.1.311.17.3.20";
						}
					}
					break;
				}
				case 20:
				{
					byte b9 = contents[8];
					if (b9 == 2)
					{
						byte b10 = contents[9];
						if (b10 == 3)
						{
							return "1.3.6.1.4.1.311.20.2.3";
						}
					}
					break;
				}
				case 88:
				{
					byte b9 = contents[8];
					if (b9 == 2)
					{
						switch (contents[9])
						{
						case 1:
							return "1.3.6.1.4.1.311.88.2.1";
						case 2:
							return "1.3.6.1.4.1.311.88.2.2";
						}
					}
					break;
				}
				}
				break;
			}
			}
			break;
		case 5:
		{
			byte b = contents[0];
			if (b != 43)
			{
				break;
			}
			switch (contents[1])
			{
			case 14:
			{
				byte b3 = contents[2];
				if (b3 != 3)
				{
					break;
				}
				byte b4 = contents[3];
				if (b4 == 2)
				{
					switch (contents[4])
					{
					case 26:
						return "1.3.14.3.2.26";
					case 7:
						return "1.3.14.3.2.7";
					}
				}
				break;
			}
			case 129:
			{
				byte b3 = contents[2];
				if (b3 == 4 && contents[3] == 0)
				{
					switch (contents[4])
					{
					case 34:
						return "1.3.132.0.34";
					case 35:
						return "1.3.132.0.35";
					}
				}
				break;
			}
			}
			break;
		}
		case 3:
		{
			byte b = contents[0];
			if (b != 85)
			{
				break;
			}
			switch (contents[1])
			{
			case 4:
				switch (contents[2])
				{
				case 3:
					return "2.5.4.3";
				case 5:
					return "2.5.4.5";
				case 6:
					return "2.5.4.6";
				case 7:
					return "2.5.4.7";
				case 8:
					return "2.5.4.8";
				case 10:
					return "2.5.4.10";
				case 11:
					return "2.5.4.11";
				case 97:
					return "2.5.4.97";
				}
				break;
			case 29:
				switch (contents[2])
				{
				case 14:
					return "2.5.29.14";
				case 15:
					return "2.5.29.15";
				case 17:
					return "2.5.29.17";
				case 19:
					return "2.5.29.19";
				case 20:
					return "2.5.29.20";
				case 35:
					return "2.5.29.35";
				}
				break;
			}
			break;
		}
		case 6:
		{
			byte b = contents[0];
			if (b != 103)
			{
				break;
			}
			byte b2 = contents[1];
			if (b2 != 129)
			{
				break;
			}
			byte b3 = contents[2];
			if (b3 != 12)
			{
				break;
			}
			byte b4 = contents[3];
			if (b4 != 1)
			{
				break;
			}
			byte b5 = contents[4];
			if (b5 == 2)
			{
				switch (contents[5])
				{
				case 1:
					return "2.23.140.1.2.1";
				case 2:
					return "2.23.140.1.2.2";
				}
			}
			break;
		}
		}
		return null;
	}

	internal static ReadOnlySpan<byte> GetContents(ReadOnlySpan<char> value)
	{
		ReadOnlySpan<byte> readOnlySpan = new byte[779]
		{
			42, 134, 72, 206, 56, 4, 1, 42, 134, 72,
			206, 56, 4, 3, 42, 134, 72, 206, 61, 2,
			1, 42, 134, 72, 206, 61, 1, 1, 42, 134,
			72, 206, 61, 1, 2, 42, 134, 72, 206, 61,
			3, 1, 7, 42, 134, 72, 206, 61, 4, 1,
			42, 134, 72, 206, 61, 4, 3, 2, 42, 134,
			72, 206, 61, 4, 3, 3, 42, 134, 72, 206,
			61, 4, 3, 4, 42, 134, 72, 134, 247, 13,
			1, 1, 1, 42, 134, 72, 134, 247, 13, 1,
			1, 5, 42, 134, 72, 134, 247, 13, 1, 1,
			7, 42, 134, 72, 134, 247, 13, 1, 1, 8,
			42, 134, 72, 134, 247, 13, 1, 1, 9, 42,
			134, 72, 134, 247, 13, 1, 1, 10, 42, 134,
			72, 134, 247, 13, 1, 1, 11, 42, 134, 72,
			134, 247, 13, 1, 1, 12, 42, 134, 72, 134,
			247, 13, 1, 1, 13, 42, 134, 72, 134, 247,
			13, 1, 5, 3, 42, 134, 72, 134, 247, 13,
			1, 5, 10, 42, 134, 72, 134, 247, 13, 1,
			5, 11, 42, 134, 72, 134, 247, 13, 1, 5,
			12, 42, 134, 72, 134, 247, 13, 1, 5, 13,
			42, 134, 72, 134, 247, 13, 1, 7, 1, 42,
			134, 72, 134, 247, 13, 1, 7, 2, 42, 134,
			72, 134, 247, 13, 1, 7, 3, 42, 134, 72,
			134, 247, 13, 1, 7, 6, 42, 134, 72, 134,
			247, 13, 1, 9, 1, 42, 134, 72, 134, 247,
			13, 1, 9, 3, 42, 134, 72, 134, 247, 13,
			1, 9, 4, 42, 134, 72, 134, 247, 13, 1,
			9, 5, 42, 134, 72, 134, 247, 13, 1, 9,
			6, 42, 134, 72, 134, 247, 13, 1, 9, 7,
			42, 134, 72, 134, 247, 13, 1, 9, 14, 42,
			134, 72, 134, 247, 13, 1, 9, 15, 42, 134,
			72, 134, 247, 13, 1, 9, 16, 1, 4, 42,
			134, 72, 134, 247, 13, 1, 9, 16, 2, 12,
			42, 134, 72, 134, 247, 13, 1, 9, 16, 2,
			14, 42, 134, 72, 134, 247, 13, 1, 9, 16,
			2, 47, 42, 134, 72, 134, 247, 13, 1, 9,
			20, 42, 134, 72, 134, 247, 13, 1, 9, 21,
			42, 134, 72, 134, 247, 13, 1, 9, 22, 1,
			42, 134, 72, 134, 247, 13, 1, 12, 1, 3,
			42, 134, 72, 134, 247, 13, 1, 12, 1, 5,
			42, 134, 72, 134, 247, 13, 1, 12, 1, 6,
			42, 134, 72, 134, 247, 13, 1, 12, 10, 1,
			1, 42, 134, 72, 134, 247, 13, 1, 12, 10,
			1, 2, 42, 134, 72, 134, 247, 13, 1, 12,
			10, 1, 3, 42, 134, 72, 134, 247, 13, 1,
			12, 10, 1, 5, 42, 134, 72, 134, 247, 13,
			1, 12, 10, 1, 6, 42, 134, 72, 134, 247,
			13, 2, 5, 42, 134, 72, 134, 247, 13, 2,
			7, 42, 134, 72, 134, 247, 13, 2, 9, 42,
			134, 72, 134, 247, 13, 2, 10, 42, 134, 72,
			134, 247, 13, 2, 11, 42, 134, 72, 134, 247,
			13, 3, 2, 42, 134, 72, 134, 247, 13, 3,
			7, 43, 6, 1, 4, 1, 130, 55, 17, 1,
			43, 6, 1, 4, 1, 130, 55, 17, 3, 20,
			43, 6, 1, 4, 1, 130, 55, 20, 2, 3,
			43, 6, 1, 4, 1, 130, 55, 88, 2, 1,
			43, 6, 1, 4, 1, 130, 55, 88, 2, 2,
			43, 6, 1, 5, 5, 7, 3, 1, 43, 6,
			1, 5, 5, 7, 3, 2, 43, 6, 1, 5,
			5, 7, 3, 3, 43, 6, 1, 5, 5, 7,
			3, 4, 43, 6, 1, 5, 5, 7, 3, 8,
			43, 6, 1, 5, 5, 7, 3, 9, 43, 6,
			1, 5, 5, 7, 6, 2, 43, 6, 1, 5,
			5, 7, 48, 1, 43, 6, 1, 5, 5, 7,
			48, 1, 2, 43, 6, 1, 5, 5, 7, 48,
			2, 43, 14, 3, 2, 26, 43, 14, 3, 2,
			7, 43, 129, 4, 0, 34, 43, 129, 4, 0,
			35, 85, 4, 3, 85, 4, 5, 85, 4, 6,
			85, 4, 7, 85, 4, 8, 85, 4, 10, 85,
			4, 11, 85, 4, 97, 85, 29, 14, 85, 29,
			15, 85, 29, 17, 85, 29, 19, 85, 29, 20,
			85, 29, 35, 96, 134, 72, 1, 101, 3, 4,
			1, 2, 96, 134, 72, 1, 101, 3, 4, 1,
			22, 96, 134, 72, 1, 101, 3, 4, 1, 42,
			96, 134, 72, 1, 101, 3, 4, 2, 1, 96,
			134, 72, 1, 101, 3, 4, 2, 2, 96, 134,
			72, 1, 101, 3, 4, 2, 3, 103, 129, 12,
			1, 2, 1, 103, 129, 12, 1, 2, 2
		};
		return value switch
		{
			"1.2.840.10040.4.1" => readOnlySpan.Slice(0, 7), 
			"1.2.840.10040.4.3" => readOnlySpan.Slice(7, 7), 
			"1.2.840.10045.2.1" => readOnlySpan.Slice(14, 7), 
			"1.2.840.10045.1.1" => readOnlySpan.Slice(21, 7), 
			"1.2.840.10045.1.2" => readOnlySpan.Slice(28, 7), 
			"1.2.840.10045.3.1.7" => readOnlySpan.Slice(35, 8), 
			"1.2.840.10045.4.1" => readOnlySpan.Slice(43, 7), 
			"1.2.840.10045.4.3.2" => readOnlySpan.Slice(50, 8), 
			"1.2.840.10045.4.3.3" => readOnlySpan.Slice(58, 8), 
			"1.2.840.10045.4.3.4" => readOnlySpan.Slice(66, 8), 
			"1.2.840.113549.1.1.1" => readOnlySpan.Slice(74, 9), 
			"1.2.840.113549.1.1.5" => readOnlySpan.Slice(83, 9), 
			"1.2.840.113549.1.1.7" => readOnlySpan.Slice(92, 9), 
			"1.2.840.113549.1.1.8" => readOnlySpan.Slice(101, 9), 
			"1.2.840.113549.1.1.9" => readOnlySpan.Slice(110, 9), 
			"1.2.840.113549.1.1.10" => readOnlySpan.Slice(119, 9), 
			"1.2.840.113549.1.1.11" => readOnlySpan.Slice(128, 9), 
			"1.2.840.113549.1.1.12" => readOnlySpan.Slice(137, 9), 
			"1.2.840.113549.1.1.13" => readOnlySpan.Slice(146, 9), 
			"1.2.840.113549.1.5.3" => readOnlySpan.Slice(155, 9), 
			"1.2.840.113549.1.5.10" => readOnlySpan.Slice(164, 9), 
			"1.2.840.113549.1.5.11" => readOnlySpan.Slice(173, 9), 
			"1.2.840.113549.1.5.12" => readOnlySpan.Slice(182, 9), 
			"1.2.840.113549.1.5.13" => readOnlySpan.Slice(191, 9), 
			"1.2.840.113549.1.7.1" => readOnlySpan.Slice(200, 9), 
			"1.2.840.113549.1.7.2" => readOnlySpan.Slice(209, 9), 
			"1.2.840.113549.1.7.3" => readOnlySpan.Slice(218, 9), 
			"1.2.840.113549.1.7.6" => readOnlySpan.Slice(227, 9), 
			"1.2.840.113549.1.9.1" => readOnlySpan.Slice(236, 9), 
			"1.2.840.113549.1.9.3" => readOnlySpan.Slice(245, 9), 
			"1.2.840.113549.1.9.4" => readOnlySpan.Slice(254, 9), 
			"1.2.840.113549.1.9.5" => readOnlySpan.Slice(263, 9), 
			"1.2.840.113549.1.9.6" => readOnlySpan.Slice(272, 9), 
			"1.2.840.113549.1.9.7" => readOnlySpan.Slice(281, 9), 
			"1.2.840.113549.1.9.14" => readOnlySpan.Slice(290, 9), 
			"1.2.840.113549.1.9.15" => readOnlySpan.Slice(299, 9), 
			"1.2.840.113549.1.9.16.1.4" => readOnlySpan.Slice(308, 11), 
			"1.2.840.113549.1.9.16.2.12" => readOnlySpan.Slice(319, 11), 
			"1.2.840.113549.1.9.16.2.14" => readOnlySpan.Slice(330, 11), 
			"1.2.840.113549.1.9.16.2.47" => readOnlySpan.Slice(341, 11), 
			"1.2.840.113549.1.9.20" => readOnlySpan.Slice(352, 9), 
			"1.2.840.113549.1.9.21" => readOnlySpan.Slice(361, 9), 
			"1.2.840.113549.1.9.22.1" => readOnlySpan.Slice(370, 10), 
			"1.2.840.113549.1.12.1.3" => readOnlySpan.Slice(380, 10), 
			"1.2.840.113549.1.12.1.5" => readOnlySpan.Slice(390, 10), 
			"1.2.840.113549.1.12.1.6" => readOnlySpan.Slice(400, 10), 
			"1.2.840.113549.1.12.10.1.1" => readOnlySpan.Slice(410, 11), 
			"1.2.840.113549.1.12.10.1.2" => readOnlySpan.Slice(421, 11), 
			"1.2.840.113549.1.12.10.1.3" => readOnlySpan.Slice(432, 11), 
			"1.2.840.113549.1.12.10.1.5" => readOnlySpan.Slice(443, 11), 
			"1.2.840.113549.1.12.10.1.6" => readOnlySpan.Slice(454, 11), 
			"1.2.840.113549.2.5" => readOnlySpan.Slice(465, 8), 
			"1.2.840.113549.2.7" => readOnlySpan.Slice(473, 8), 
			"1.2.840.113549.2.9" => readOnlySpan.Slice(481, 8), 
			"1.2.840.113549.2.10" => readOnlySpan.Slice(489, 8), 
			"1.2.840.113549.2.11" => readOnlySpan.Slice(497, 8), 
			"1.2.840.113549.3.2" => readOnlySpan.Slice(505, 8), 
			"1.2.840.113549.3.7" => readOnlySpan.Slice(513, 8), 
			"1.3.6.1.4.1.311.17.1" => readOnlySpan.Slice(521, 9), 
			"1.3.6.1.4.1.311.17.3.20" => readOnlySpan.Slice(530, 10), 
			"1.3.6.1.4.1.311.20.2.3" => readOnlySpan.Slice(540, 10), 
			"1.3.6.1.4.1.311.88.2.1" => readOnlySpan.Slice(550, 10), 
			"1.3.6.1.4.1.311.88.2.2" => readOnlySpan.Slice(560, 10), 
			"1.3.6.1.5.5.7.3.1" => readOnlySpan.Slice(570, 8), 
			"1.3.6.1.5.5.7.3.2" => readOnlySpan.Slice(578, 8), 
			"1.3.6.1.5.5.7.3.3" => readOnlySpan.Slice(586, 8), 
			"1.3.6.1.5.5.7.3.4" => readOnlySpan.Slice(594, 8), 
			"1.3.6.1.5.5.7.3.8" => readOnlySpan.Slice(602, 8), 
			"1.3.6.1.5.5.7.3.9" => readOnlySpan.Slice(610, 8), 
			"1.3.6.1.5.5.7.6.2" => readOnlySpan.Slice(618, 8), 
			"1.3.6.1.5.5.7.48.1" => readOnlySpan.Slice(626, 8), 
			"1.3.6.1.5.5.7.48.1.2" => readOnlySpan.Slice(634, 9), 
			"1.3.6.1.5.5.7.48.2" => readOnlySpan.Slice(643, 8), 
			"1.3.14.3.2.26" => readOnlySpan.Slice(651, 5), 
			"1.3.14.3.2.7" => readOnlySpan.Slice(656, 5), 
			"1.3.132.0.34" => readOnlySpan.Slice(661, 5), 
			"1.3.132.0.35" => readOnlySpan.Slice(666, 5), 
			"2.5.4.3" => readOnlySpan.Slice(671, 3), 
			"2.5.4.5" => readOnlySpan.Slice(674, 3), 
			"2.5.4.6" => readOnlySpan.Slice(677, 3), 
			"2.5.4.7" => readOnlySpan.Slice(680, 3), 
			"2.5.4.8" => readOnlySpan.Slice(683, 3), 
			"2.5.4.10" => readOnlySpan.Slice(686, 3), 
			"2.5.4.11" => readOnlySpan.Slice(689, 3), 
			"2.5.4.97" => readOnlySpan.Slice(692, 3), 
			"2.5.29.14" => readOnlySpan.Slice(695, 3), 
			"2.5.29.15" => readOnlySpan.Slice(698, 3), 
			"2.5.29.17" => readOnlySpan.Slice(701, 3), 
			"2.5.29.19" => readOnlySpan.Slice(704, 3), 
			"2.5.29.20" => readOnlySpan.Slice(707, 3), 
			"2.5.29.35" => readOnlySpan.Slice(710, 3), 
			"2.16.840.1.101.3.4.1.2" => readOnlySpan.Slice(713, 9), 
			"2.16.840.1.101.3.4.1.22" => readOnlySpan.Slice(722, 9), 
			"2.16.840.1.101.3.4.1.42" => readOnlySpan.Slice(731, 9), 
			"2.16.840.1.101.3.4.2.1" => readOnlySpan.Slice(740, 9), 
			"2.16.840.1.101.3.4.2.2" => readOnlySpan.Slice(749, 9), 
			"2.16.840.1.101.3.4.2.3" => readOnlySpan.Slice(758, 9), 
			"2.23.140.1.2.1" => readOnlySpan.Slice(767, 6), 
			"2.23.140.1.2.2" => readOnlySpan.Slice(773, 6), 
			_ => ReadOnlySpan<byte>.Empty, 
		};
	}
}

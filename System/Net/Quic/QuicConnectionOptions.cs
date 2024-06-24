using System.Threading;

namespace System.Net.Quic;

public abstract class QuicConnectionOptions
{
	public int MaxInboundBidirectionalStreams { get; set; }

	public int MaxInboundUnidirectionalStreams { get; set; }

	public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;


	public long DefaultStreamErrorCode { get; set; } = -1L;


	public long DefaultCloseErrorCode { get; set; } = -1L;


	internal QuicConnectionOptions()
	{
	}

	internal virtual void Validate(string argumentName)
	{
		if (MaxInboundBidirectionalStreams < 0 || MaxInboundBidirectionalStreams > 65535)
		{
			throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.net_quic_in_range, "MaxInboundBidirectionalStreams", ushort.MaxValue), argumentName);
		}
		if (MaxInboundUnidirectionalStreams < 0 || MaxInboundUnidirectionalStreams > 65535)
		{
			throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.net_quic_in_range, "MaxInboundUnidirectionalStreams", ushort.MaxValue), argumentName);
		}
		if (IdleTimeout < TimeSpan.Zero && IdleTimeout != Timeout.InfiniteTimeSpan)
		{
			throw new ArgumentOutOfRangeException("IdleTimeout", System.SR.net_quic_timeout_use_gt_zero);
		}
		if (DefaultStreamErrorCode < 0 || DefaultStreamErrorCode > 4611686018427387903L)
		{
			throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.net_quic_in_range, "DefaultStreamErrorCode", 4611686018427387903L), argumentName);
		}
		if (DefaultCloseErrorCode < 0 || DefaultCloseErrorCode > 4611686018427387903L)
		{
			throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.net_quic_in_range, "DefaultCloseErrorCode", 4611686018427387903L), argumentName);
		}
	}
}

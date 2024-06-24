using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions;

internal ref struct RegexWriter
{
	private readonly RegexTree _tree;

	private readonly Dictionary<string, int> _stringTable;

	private System.Collections.Generic.ValueListBuilder<int> _emitted;

	private System.Collections.Generic.ValueListBuilder<int> _intStack;

	private int _trackCount;

	private RegexWriter(RegexTree tree, Span<int> emittedSpan, Span<int> intStackSpan)
	{
		_tree = tree;
		_emitted = new System.Collections.Generic.ValueListBuilder<int>(emittedSpan);
		_intStack = new System.Collections.Generic.ValueListBuilder<int>(intStackSpan);
		_stringTable = new Dictionary<string, int>();
		_trackCount = 0;
	}

	public void Dispose()
	{
		_emitted.Dispose();
		_intStack.Dispose();
	}

	public static RegexInterpreterCode Write(RegexTree tree)
	{
		Span<int> emittedSpan = stackalloc int[64];
		Span<int> intStackSpan = stackalloc int[32];
		using RegexWriter regexWriter = new RegexWriter(tree, emittedSpan, intStackSpan);
		return regexWriter.EmitCode();
	}

	private RegexInterpreterCode EmitCode()
	{
		Emit(RegexOpcode.Lazybranch, 0);
		RegexNode regexNode = _tree.Root;
		int num = 0;
		while (true)
		{
			int num2 = regexNode.ChildCount();
			if (num2 == 0)
			{
				EmitFragment(regexNode.Kind, regexNode, 0);
			}
			else if (num < num2)
			{
				EmitFragment(regexNode.Kind | (RegexNodeKind)64, regexNode, num);
				regexNode = regexNode.Child(num);
				_intStack.Append(num);
				num = 0;
				continue;
			}
			if (_intStack.Length == 0)
			{
				break;
			}
			num = _intStack.Pop();
			regexNode = regexNode.Parent;
			EmitFragment(regexNode.Kind | (RegexNodeKind)128, regexNode, num);
			num++;
		}
		PatchJump(0, _emitted.Length);
		Emit(RegexOpcode.Stop);
		int[] codes = _emitted.AsSpan().ToArray();
		string[] array = new string[_stringTable.Count];
		foreach (KeyValuePair<string, int> item in _stringTable)
		{
			array[item.Value] = item.Key;
		}
		return new RegexInterpreterCode(_tree.FindOptimizations, _tree.Options, codes, array, _trackCount);
	}

	private void PatchJump(int offset, int jumpDest)
	{
		_emitted[offset + 1] = jumpDest;
	}

	private void Emit(RegexOpcode op)
	{
		if (RegexInterpreterCode.OpcodeBacktracks(op))
		{
			_trackCount++;
		}
		_emitted.Append((int)op);
	}

	private void Emit(RegexOpcode op, int opd1)
	{
		if (RegexInterpreterCode.OpcodeBacktracks(op))
		{
			_trackCount++;
		}
		_emitted.Append((int)op);
		_emitted.Append(opd1);
	}

	private void Emit(RegexOpcode op, int opd1, int opd2)
	{
		if (RegexInterpreterCode.OpcodeBacktracks(op))
		{
			_trackCount++;
		}
		_emitted.Append((int)op);
		_emitted.Append(opd1);
		_emitted.Append(opd2);
	}

	private int StringCode(string str)
	{
		bool exists;
		ref int valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringTable, str, out exists);
		if (!exists)
		{
			valueRefOrAddDefault = _stringTable.Count - 1;
		}
		return valueRefOrAddDefault;
	}

	private void EmitFragment(RegexNodeKind nodeType, RegexNode node, int curIndex)
	{
		RegexOpcode regexOpcode = RegexOpcode.Onerep;
		if ((node.Options & RegexOptions.RightToLeft) != 0)
		{
			regexOpcode |= RegexOpcode.RightToLeft;
		}
		if ((node.Options & RegexOptions.IgnoreCase) != 0)
		{
			regexOpcode |= RegexOpcode.CaseInsensitive;
		}
		switch (nodeType)
		{
		case (RegexNodeKind)88:
			if (curIndex < node.ChildCount() - 1)
			{
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Lazybranch, 0);
			}
			break;
		case (RegexNodeKind)152:
			if (curIndex < node.ChildCount() - 1)
			{
				int offset3 = _intStack.Pop();
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Goto, 0);
				PatchJump(offset3, _emitted.Length);
			}
			else
			{
				for (int i = 0; i < curIndex; i++)
				{
					PatchJump(_intStack.Pop(), _emitted.Length);
				}
			}
			break;
		case (RegexNodeKind)97:
			if (curIndex == 0)
			{
				Emit(RegexOpcode.Setjump);
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Lazybranch, 0);
				Emit(RegexOpcode.TestBackreference, RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping));
				Emit(RegexOpcode.Forejump);
			}
			break;
		case (RegexNodeKind)161:
			switch (curIndex)
			{
			case 0:
			{
				int offset2 = _intStack.Pop();
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Goto, 0);
				PatchJump(offset2, _emitted.Length);
				Emit(RegexOpcode.Forejump);
				break;
			}
			case 1:
				PatchJump(_intStack.Pop(), _emitted.Length);
				break;
			}
			break;
		case (RegexNodeKind)98:
			if (curIndex == 0)
			{
				Emit(RegexOpcode.Setjump);
				Emit(RegexOpcode.Setmark);
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Lazybranch, 0);
			}
			break;
		case (RegexNodeKind)162:
			switch (curIndex)
			{
			case 0:
				Emit(RegexOpcode.Getmark);
				Emit(RegexOpcode.Forejump);
				break;
			case 1:
			{
				int offset = _intStack.Pop();
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Goto, 0);
				PatchJump(offset, _emitted.Length);
				Emit(RegexOpcode.Getmark);
				Emit(RegexOpcode.Forejump);
				break;
			}
			case 2:
				PatchJump(_intStack.Pop(), _emitted.Length);
				break;
			}
			break;
		case (RegexNodeKind)90:
		case (RegexNodeKind)91:
			if (node.N < int.MaxValue || node.M > 1)
			{
				Emit((node.M == 0) ? RegexOpcode.Nullcount : RegexOpcode.Setcount, (node.M != 0) ? (1 - node.M) : 0);
			}
			else
			{
				Emit((node.M == 0) ? RegexOpcode.Nullmark : RegexOpcode.Setmark);
			}
			if (node.M == 0)
			{
				_intStack.Append(_emitted.Length);
				Emit(RegexOpcode.Goto, 0);
			}
			_intStack.Append(_emitted.Length);
			break;
		case (RegexNodeKind)154:
		case (RegexNodeKind)155:
		{
			int length = _emitted.Length;
			int num = (int)(nodeType - 154);
			if (node.N < int.MaxValue || node.M > 1)
			{
				Emit((RegexOpcode)(28 + num), _intStack.Pop(), (node.N == int.MaxValue) ? int.MaxValue : (node.N - node.M));
			}
			else
			{
				Emit((RegexOpcode)(24 + num), _intStack.Pop());
			}
			if (node.M == 0)
			{
				PatchJump(_intStack.Pop(), length);
			}
			break;
		}
		case (RegexNodeKind)92:
			Emit(RegexOpcode.Setmark);
			break;
		case (RegexNodeKind)156:
			Emit(RegexOpcode.Capturemark, RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping), RegexParser.MapCaptureNumber(node.N, _tree.CaptureNumberSparseMapping));
			break;
		case (RegexNodeKind)94:
			Emit(RegexOpcode.Setjump);
			Emit(RegexOpcode.Setmark);
			break;
		case (RegexNodeKind)158:
			Emit(RegexOpcode.Getmark);
			Emit(RegexOpcode.Forejump);
			break;
		case (RegexNodeKind)95:
			Emit(RegexOpcode.Setjump);
			_intStack.Append(_emitted.Length);
			Emit(RegexOpcode.Lazybranch, 0);
			break;
		case (RegexNodeKind)159:
			Emit(RegexOpcode.Backjump);
			PatchJump(_intStack.Pop(), _emitted.Length);
			Emit(RegexOpcode.Forejump);
			break;
		case (RegexNodeKind)96:
			Emit(RegexOpcode.Setjump);
			break;
		case (RegexNodeKind)160:
			Emit(RegexOpcode.Forejump);
			break;
		case RegexNodeKind.One:
		case RegexNodeKind.Notone:
			Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), node.Ch);
			break;
		case RegexNodeKind.Oneloop:
		case RegexNodeKind.Notoneloop:
		case RegexNodeKind.Onelazy:
		case RegexNodeKind.Notonelazy:
		case RegexNodeKind.Oneloopatomic:
		case RegexNodeKind.Notoneloopatomic:
			if (node.M > 0)
			{
				RegexNodeKind kind = node.Kind;
				bool flag = ((kind == RegexNodeKind.Oneloop || kind == RegexNodeKind.Onelazy || kind == RegexNodeKind.Oneloopatomic) ? true : false);
				Emit((RegexOpcode)(((!flag) ? 1 : 0) | (int)regexOpcode), node.Ch, node.M);
			}
			if (node.N > node.M)
			{
				Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), node.Ch, (node.N == int.MaxValue) ? int.MaxValue : (node.N - node.M));
			}
			break;
		case RegexNodeKind.Setloop:
		case RegexNodeKind.Setlazy:
		case RegexNodeKind.Setloopatomic:
		{
			int opd = StringCode(node.Str);
			if (node.M > 0)
			{
				Emit(RegexOpcode.Setrep | regexOpcode, opd, node.M);
			}
			if (node.N > node.M)
			{
				Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), opd, (node.N == int.MaxValue) ? int.MaxValue : (node.N - node.M));
			}
			break;
		}
		case RegexNodeKind.Multi:
			Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), StringCode(node.Str));
			break;
		case RegexNodeKind.Set:
			Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), StringCode(node.Str));
			break;
		case RegexNodeKind.Backreference:
			Emit((RegexOpcode)((int)node.Kind | (int)regexOpcode), RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping));
			break;
		case RegexNodeKind.Bol:
		case RegexNodeKind.Eol:
		case RegexNodeKind.Boundary:
		case RegexNodeKind.NonBoundary:
		case RegexNodeKind.Beginning:
		case RegexNodeKind.Start:
		case RegexNodeKind.EndZ:
		case RegexNodeKind.End:
		case RegexNodeKind.Nothing:
		case RegexNodeKind.ECMABoundary:
		case RegexNodeKind.NonECMABoundary:
		case RegexNodeKind.UpdateBumpalong:
			Emit((RegexOpcode)node.Kind);
			break;
		}
	}
}

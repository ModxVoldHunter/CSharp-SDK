using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class RegexNodeConverter
{
	private readonly Hashtable _captureSparseMapping;

	internal readonly SymbolicRegexBuilder<BDD> _builder;

	private Dictionary<string, BDD> _setBddCache;

	public RegexNodeConverter(SymbolicRegexBuilder<BDD> builder, Hashtable captureSparseMapping)
	{
		_builder = builder;
		_captureSparseMapping = captureSparseMapping;
	}

	internal SymbolicRegexNode<BDD> ConvertToSymbolicRegexNode(RegexNode root)
	{
		DoublyLinkedList<SymbolicRegexNode<BDD>> doublyLinkedList = new DoublyLinkedList<SymbolicRegexNode<BDD>>();
		Stack<(RegexNode, DoublyLinkedList<SymbolicRegexNode<BDD>>, DoublyLinkedList<SymbolicRegexNode<BDD>>[])> stack = new Stack<(RegexNode, DoublyLinkedList<SymbolicRegexNode<BDD>>, DoublyLinkedList<SymbolicRegexNode<BDD>>[])>();
		stack.Push((root, doublyLinkedList, CreateChildResultArray(root.ChildCount())));
		(RegexNode, DoublyLinkedList<SymbolicRegexNode<BDD>>, DoublyLinkedList<SymbolicRegexNode<BDD>>[]) result;
		while (stack.TryPop(out result))
		{
			var (regexNode, doublyLinkedList2, array) = result;
			if (array == null || array[0] == null)
			{
				switch (regexNode.Kind)
				{
				case RegexNodeKind.Empty:
				case RegexNodeKind.UpdateBumpalong:
					continue;
				case RegexNodeKind.One:
					doublyLinkedList2.AddLast(_builder.CreateSingleton(_builder._charSetSolver.CreateBDDFromChar(regexNode.Ch)));
					continue;
				case RegexNodeKind.Notone:
					doublyLinkedList2.AddLast(_builder.CreateSingleton(_builder._solver.Not(_builder._charSetSolver.CreateBDDFromChar(regexNode.Ch))));
					continue;
				case RegexNodeKind.Set:
					doublyLinkedList2.AddLast(ConvertSet(regexNode));
					continue;
				case RegexNodeKind.Multi:
				{
					string str2 = regexNode.Str;
					string text = str2;
					foreach (char c in text)
					{
						doublyLinkedList2.AddLast(_builder.CreateSingleton(_builder._charSetSolver.CreateBDDFromChar(c)));
					}
					continue;
				}
				case RegexNodeKind.Capture:
					if (regexNode.N != -1)
					{
						break;
					}
					goto case RegexNodeKind.Alternate;
				case RegexNodeKind.Alternate:
				case RegexNodeKind.Concatenate:
				case RegexNodeKind.Loop:
				case RegexNodeKind.Lazyloop:
				{
					stack.Push(result);
					for (int i = 0; i < regexNode.ChildCount(); i++)
					{
						array[i] = new DoublyLinkedList<SymbolicRegexNode<BDD>>();
						stack.Push((regexNode.Child(i), array[i], CreateChildResultArray(regexNode.Child(i).ChildCount())));
					}
					continue;
				}
				case RegexNodeKind.Oneloop:
				case RegexNodeKind.Notoneloop:
				case RegexNodeKind.Onelazy:
				case RegexNodeKind.Notonelazy:
				{
					BDD set2 = _builder._charSetSolver.CreateBDDFromChar(regexNode.Ch);
					if (regexNode.IsNotoneFamily)
					{
						set2 = _builder._solver.Not(set2);
					}
					DoublyLinkedList<SymbolicRegexNode<BDD>> doublyLinkedList3 = doublyLinkedList2;
					SymbolicRegexBuilder<BDD> builder = _builder;
					SymbolicRegexNode<BDD> node2 = _builder.CreateSingleton(set2);
					RegexNodeKind kind = regexNode.Kind;
					bool isLazy = kind - 6 <= (RegexNodeKind)1;
					doublyLinkedList3.AddLast(builder.CreateLoop(node2, isLazy, regexNode.M, regexNode.N));
					continue;
				}
				case RegexNodeKind.Setloop:
				case RegexNodeKind.Setlazy:
				{
					string str = regexNode.Str;
					BDD set = CreateBDDFromSetString(str);
					doublyLinkedList2.AddLast(_builder.CreateLoop(_builder.CreateSingleton(set), regexNode.Kind == RegexNodeKind.Setlazy, regexNode.M, regexNode.N));
					continue;
				}
				case RegexNodeKind.Nothing:
					doublyLinkedList2.AddLast(_builder._nothing);
					continue;
				case RegexNodeKind.Beginning:
					doublyLinkedList2.AddLast(_builder.BeginningAnchor);
					continue;
				case RegexNodeKind.Bol:
					EnsureNewlinePredicateInitialized();
					doublyLinkedList2.AddLast(_builder.BolAnchor);
					continue;
				case RegexNodeKind.End:
					doublyLinkedList2.AddLast(_builder.EndAnchor);
					continue;
				case RegexNodeKind.EndZ:
					EnsureNewlinePredicateInitialized();
					doublyLinkedList2.AddLast(_builder.EndAnchorZ);
					continue;
				case RegexNodeKind.Eol:
					EnsureNewlinePredicateInitialized();
					doublyLinkedList2.AddLast(_builder.EolAnchor);
					continue;
				case RegexNodeKind.Boundary:
					EnsureWordLetterPredicateInitialized();
					doublyLinkedList2.AddLast(_builder.BoundaryAnchor);
					continue;
				case RegexNodeKind.NonBoundary:
					EnsureWordLetterPredicateInitialized();
					doublyLinkedList2.AddLast(_builder.NonBoundaryAnchor);
					continue;
				}
				string notSupported_NonBacktrackingConflictingExpression = System.SR.NotSupported_NonBacktrackingConflictingExpression;
				string p;
				switch (regexNode.Kind)
				{
				case RegexNodeKind.Atomic:
				case RegexNodeKind.Oneloopatomic:
				case RegexNodeKind.Notoneloopatomic:
				case RegexNodeKind.Setloopatomic:
					p = System.SR.ExpressionDescription_AtomicSubexpressions;
					break;
				case RegexNodeKind.Backreference:
					p = System.SR.ExpressionDescription_Backreference;
					break;
				case RegexNodeKind.BackreferenceConditional:
					p = System.SR.ExpressionDescription_Conditional;
					break;
				case RegexNodeKind.Capture:
					p = System.SR.ExpressionDescription_BalancingGroup;
					break;
				case RegexNodeKind.ExpressionConditional:
					p = System.SR.ExpressionDescription_IfThenElse;
					break;
				case RegexNodeKind.NegativeLookaround:
					p = System.SR.ExpressionDescription_NegativeLookaround;
					break;
				case RegexNodeKind.PositiveLookaround:
					p = System.SR.ExpressionDescription_PositiveLookaround;
					break;
				case RegexNodeKind.Start:
					p = System.SR.ExpressionDescription_ContiguousMatches;
					break;
				default:
					p = UnexpectedNodeType(regexNode);
					break;
				}
				throw new NotSupportedException(System.SR.Format(notSupported_NonBacktrackingConflictingExpression, p));
			}
			switch (regexNode.Kind)
			{
			case RegexNodeKind.Concatenate:
			{
				DoublyLinkedList<SymbolicRegexNode<BDD>>[] array2 = array;
				foreach (DoublyLinkedList<SymbolicRegexNode<BDD>> other in array2)
				{
					doublyLinkedList2.AddLast(other);
				}
				break;
			}
			case RegexNodeKind.Alternate:
			{
				SymbolicRegexNode<BDD> symbolicRegexNode = _builder._nothing;
				for (int num = array.Length - 1; num >= 0; num--)
				{
					DoublyLinkedList<SymbolicRegexNode<BDD>> doublyLinkedList6 = array[num];
					SymbolicRegexNode<BDD> symbolicRegexNode2 = ((doublyLinkedList6.Count == 1) ? doublyLinkedList6.FirstElement : _builder.CreateConcatAlreadyReversed(doublyLinkedList6));
					if (!symbolicRegexNode2.IsNothing(_builder._solver))
					{
						symbolicRegexNode = (symbolicRegexNode2.IsAnyStar(_builder._solver) ? symbolicRegexNode2 : SymbolicRegexNode<BDD>.CreateAlternate(_builder, symbolicRegexNode2, symbolicRegexNode));
					}
				}
				doublyLinkedList2.AddLast(symbolicRegexNode);
				break;
			}
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
			{
				DoublyLinkedList<SymbolicRegexNode<BDD>> doublyLinkedList5 = array[0];
				SymbolicRegexNode<BDD> node3 = ((doublyLinkedList5.Count == 1) ? doublyLinkedList5.FirstElement : _builder.CreateConcatAlreadyReversed(doublyLinkedList5));
				doublyLinkedList2.AddLast(_builder.CreateLoop(node3, regexNode.Kind == RegexNodeKind.Lazyloop, regexNode.M, regexNode.N));
				break;
			}
			default:
			{
				DoublyLinkedList<SymbolicRegexNode<BDD>> doublyLinkedList4 = array[0];
				int captureNum = RegexParser.MapCaptureNumber(regexNode.M, _captureSparseMapping);
				doublyLinkedList4.AddFirst(_builder.CreateCaptureStart(captureNum));
				doublyLinkedList4.AddLast(_builder.CreateCaptureEnd(captureNum));
				doublyLinkedList2.AddLast(doublyLinkedList4);
				break;
			}
			}
		}
		if (doublyLinkedList.Count != 1)
		{
			return _builder.CreateConcatAlreadyReversed(doublyLinkedList);
		}
		return doublyLinkedList.FirstElement;
		SymbolicRegexNode<BDD> ConvertSet(RegexNode node)
		{
			string str3 = node.Str;
			return _builder.CreateSingleton(CreateBDDFromSetString(str3));
		}
		static DoublyLinkedList<SymbolicRegexNode<BDD>>[] CreateChildResultArray(int k)
		{
			if (k != 0)
			{
				return new DoublyLinkedList<SymbolicRegexNode<BDD>>[k];
			}
			return null;
		}
		void EnsureNewlinePredicateInitialized()
		{
			if (_builder._newLineSet.Equals(_builder._solver.Empty))
			{
				_builder._newLineSet = _builder._charSetSolver.CreateBDDFromChar('\n');
			}
		}
		void EnsureWordLetterPredicateInitialized()
		{
			if (_builder._wordLetterForBoundariesSet.Equals(_builder._solver.Empty))
			{
				_builder._wordLetterForBoundariesSet = UnicodeCategoryConditions.WordLetterForAnchors((CharSetSolver)_builder._solver);
			}
		}
		static string UnexpectedNodeType(RegexNode node)
		{
			return $"Unexpected ({"RegexNodeKind"}: {node.Kind})";
		}
	}

	private BDD CreateBDDFromSetString(string set)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack(CreateBDDFromSetString, set);
		}
		if (_setBddCache == null)
		{
			_setBddCache = new Dictionary<string, BDD>();
		}
		bool exists;
		ref BDD valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_setBddCache, set, out exists);
		return valueRefOrAddDefault ?? (valueRefOrAddDefault = Compute(set));
		BDD Compute(string set)
		{
			List<BDD> list = new List<BDD>();
			CharSetSolver charSetSolver = (CharSetSolver)_builder._solver;
			bool flag = RegexCharClass.IsNegated(set);
			List<(char, char)> list2 = RegexCharClass.ComputeRanges(set);
			if (list2 != null)
			{
				foreach (var item3 in list2)
				{
					char item = item3.Item1;
					char item2 = item3.Item2;
					BDD bDD = charSetSolver.CreateBDDFromRange(item, item2);
					if (flag)
					{
						bDD = charSetSolver.Not(bDD);
					}
					list.Add(bDD);
				}
			}
			Span<bool> catCodes2 = stackalloc bool[30];
			int num = set[1];
			int num2 = set[2];
			int num3 = num + 3;
			int num4 = num3;
			while (num4 < num3 + num2)
			{
				short num5 = (short)set[num4++];
				if (num5 != 0)
				{
					BDD bDD2 = MapCategoryCodeToCondition((UnicodeCategory)(Math.Abs(num5) - 1));
					if ((num5 < 0) ^ flag)
					{
						bDD2 = charSetSolver.Not(bDD2);
					}
					list.Add(bDD2);
				}
				else
				{
					num5 = (short)set[num4++];
					if (num5 != 0)
					{
						bool flag2 = num5 < 0;
						catCodes2.Clear();
						while (num5 != 0)
						{
							int index = Math.Abs((int)num5) - 1;
							catCodes2[index] = true;
							num5 = (short)set[num4++];
						}
						BDD bDD3 = MapCategoryCodeSetToCondition(catCodes2);
						if (flag ^ flag2)
						{
							bDD3 = charSetSolver.Not(bDD3);
						}
						list.Add(bDD3);
					}
				}
			}
			BDD bDD4 = null;
			if (set.Length > num4)
			{
				bDD4 = CreateBDDFromSetString(set.Substring(num4));
			}
			BDD bDD5 = ((list.Count != 0) ? (flag ? charSetSolver.And(CollectionsMarshal.AsSpan(list)) : charSetSolver.Or(CollectionsMarshal.AsSpan(list))) : (flag ? charSetSolver.Empty : charSetSolver.Full));
			if (bDD4 != null)
			{
				bDD5 = charSetSolver.And(bDD5, charSetSolver.Not(bDD4));
			}
			return bDD5;
			BDD MapCategoryCodeSetToCondition(Span<bool> catCodes)
			{
				BDD bDD6 = null;
				if (catCodes[0] && catCodes[1] && catCodes[2] && catCodes[3] && catCodes[4] && catCodes[5] && catCodes[8] && catCodes[18])
				{
					catCodes[0] = (catCodes[1] = (catCodes[2] = (catCodes[3] = (catCodes[4] = (catCodes[5] = (catCodes[8] = (catCodes[18] = false)))))));
					bDD6 = UnicodeCategoryConditions.WordLetter(charSetSolver);
				}
				for (int i = 0; i < catCodes.Length; i++)
				{
					if (catCodes[i])
					{
						BDD bDD7 = MapCategoryCodeToCondition((UnicodeCategory)i);
						bDD6 = ((bDD6 == null) ? bDD7 : charSetSolver.Or(bDD6, bDD7));
					}
				}
				return bDD6;
			}
		}
		static BDD MapCategoryCodeToCondition(UnicodeCategory code)
		{
			if (code != (UnicodeCategory)99)
			{
				return UnicodeCategoryConditions.GetCategory(code);
			}
			return UnicodeCategoryConditions.WhiteSpace;
		}
	}
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System;

internal sealed class RuntimeType : TypeInfo, ICloneable
{
	private sealed class ActivatorCache
	{
		private unsafe readonly delegate*<void*, object> _pfnAllocator;

		private unsafe readonly void* _allocatorFirstArg;

		private unsafe readonly delegate*<object, void> _pfnCtor;

		private readonly bool _ctorIsPublic;

		internal bool CtorIsPublic => _ctorIsPublic;

		internal unsafe ActivatorCache(RuntimeType rt)
		{
			rt.CreateInstanceCheckThis();
			try
			{
				RuntimeTypeHandle.GetActivationInfo(rt, out _pfnAllocator, out _allocatorFirstArg, out _pfnCtor, out _ctorIsPublic);
			}
			catch (Exception ex)
			{
				string message = SR.Format(SR.Activator_CannotCreateInstance, rt, ex.Message);
				if (!(ex is ArgumentException))
				{
					if (!(ex is PlatformNotSupportedException))
					{
						if (!(ex is NotSupportedException))
						{
							if (!(ex is MethodAccessException))
							{
								if (!(ex is MissingMethodException))
								{
									if (ex is MemberAccessException)
									{
										throw new MemberAccessException(message);
									}
									throw;
								}
								throw new MissingMethodException(message);
							}
							throw new MethodAccessException(message);
						}
						throw new NotSupportedException(message);
					}
					throw new PlatformNotSupportedException(message);
				}
				throw new ArgumentException(message);
			}
			if (_pfnAllocator == (delegate*<void*, object>)null)
			{
				_pfnAllocator = &ReturnNull;
			}
			if (_pfnCtor == (delegate*<object, void>)null)
			{
				_pfnCtor = &CtorNoopStub;
			}
			static void CtorNoopStub(object uninitializedObject)
			{
			}
			unsafe static object ReturnNull(void* _)
			{
				return null;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe object CreateUninitializedObject(RuntimeType rt)
		{
			object result = _pfnAllocator(_allocatorFirstArg);
			GC.KeepAlive(rt);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe void CallConstructor(object uninitializedObject)
		{
			_pfnCtor(uninitializedObject);
		}
	}

	internal enum MemberListType
	{
		All,
		CaseSensitive,
		CaseInsensitive,
		HandleToInfo
	}

	internal struct ListBuilder<T> where T : class
	{
		private T[] _items;

		private T _item;

		private int _count;

		private int _capacity;

		public T this[int index]
		{
			get
			{
				if (_items == null)
				{
					return _item;
				}
				return _items[index];
			}
		}

		public int Count => _count;

		public ListBuilder(int capacity)
		{
			_items = null;
			_item = null;
			_count = 0;
			_capacity = capacity;
		}

		public T[] ToArray()
		{
			if (_count == 0)
			{
				return Array.Empty<T>();
			}
			if (_count == 1)
			{
				return new T[1] { _item };
			}
			Array.Resize(ref _items, _count);
			_capacity = _count;
			return _items;
		}

		public void CopyTo(object[] array, int index)
		{
			if (_count != 0)
			{
				if (_count == 1)
				{
					array[index] = _item;
				}
				else
				{
					Array.Copy(_items, 0, array, index, _count);
				}
			}
		}

		public void Add(T item)
		{
			if (_count == 0)
			{
				_item = item;
			}
			else
			{
				if (_count == 1)
				{
					if (_capacity < 2)
					{
						_capacity = 4;
					}
					_items = new T[_capacity];
					_items[0] = _item;
				}
				else if (_capacity == _count)
				{
					int num = 2 * _capacity;
					Array.Resize(ref _items, num);
					_capacity = num;
				}
				_items[_count] = item;
			}
			_count++;
		}
	}

	internal sealed class RuntimeTypeCache
	{
		internal enum CacheType
		{
			Method,
			Constructor,
			Field,
			Property,
			Event,
			Interface,
			NestedType
		}

		private readonly struct Filter
		{
			private readonly MdUtf8String m_name;

			private readonly MemberListType m_listType;

			public unsafe Filter(byte* pUtf8Name, int cUtf8Name, MemberListType listType)
			{
				m_name = new MdUtf8String(pUtf8Name, cUtf8Name);
				m_listType = listType;
			}

			public bool Match(MdUtf8String name)
			{
				bool result = true;
				if (m_listType == MemberListType.CaseSensitive)
				{
					result = m_name.Equals(name);
				}
				else if (m_listType == MemberListType.CaseInsensitive)
				{
					result = m_name.EqualsCaseInsensitive(name);
				}
				return result;
			}

			public bool RequiresStringComparison()
			{
				if (m_listType != MemberListType.CaseSensitive)
				{
					return m_listType == MemberListType.CaseInsensitive;
				}
				return true;
			}

			public bool CaseSensitive()
			{
				return m_listType == MemberListType.CaseSensitive;
			}
		}

		private sealed class MemberInfoCache<T> where T : MemberInfo
		{
			private CerHashtable<string, T[]> m_csMemberInfos;

			private CerHashtable<string, T[]> m_cisMemberInfos;

			private T[] m_allMembers;

			private bool m_cacheComplete;

			private readonly RuntimeTypeCache m_runtimeTypeCache;

			internal RuntimeType ReflectedType => m_runtimeTypeCache.GetRuntimeType();

			internal MemberInfoCache(RuntimeTypeCache runtimeTypeCache)
			{
				m_runtimeTypeCache = runtimeTypeCache;
			}

			internal MethodBase AddMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method, CacheType cacheType)
			{
				T[] allMembers = m_allMembers;
				if (allMembers != null)
				{
					switch (cacheType)
					{
					case CacheType.Method:
					{
						T[] array2 = allMembers;
						foreach (T val2 in array2)
						{
							if ((object)val2 == null)
							{
								break;
							}
							if (val2 is RuntimeMethodInfo { MethodHandle: var methodHandle2 } runtimeMethodInfo && methodHandle2.Value == method.Value)
							{
								return runtimeMethodInfo;
							}
						}
						break;
					}
					case CacheType.Constructor:
					{
						T[] array = allMembers;
						foreach (T val in array)
						{
							if ((object)val == null)
							{
								break;
							}
							if (val is RuntimeConstructorInfo { MethodHandle: var methodHandle } runtimeConstructorInfo && methodHandle.Value == method.Value)
							{
								return runtimeConstructorInfo;
							}
						}
						break;
					}
					}
				}
				T[] list = null;
				MethodAttributes attributes = RuntimeMethodHandle.GetAttributes(method);
				bool isPublic = (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
				bool isStatic = (attributes & MethodAttributes.Static) != 0;
				bool isInherited = declaringType != ReflectedType;
				BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
				switch (cacheType)
				{
				case CacheType.Method:
					list = (T[])(object)new RuntimeMethodInfo[1]
					{
						new RuntimeMethodInfo(method, declaringType, m_runtimeTypeCache, attributes, bindingFlags, null)
					};
					break;
				case CacheType.Constructor:
					list = (T[])(object)new RuntimeConstructorInfo[1]
					{
						new RuntimeConstructorInfo(method, declaringType, m_runtimeTypeCache, attributes, bindingFlags)
					};
					break;
				}
				Insert(ref list, null, MemberListType.HandleToInfo);
				return (MethodBase)(object)list[0];
			}

			internal FieldInfo AddField(RuntimeFieldHandleInternal field)
			{
				T[] allMembers = m_allMembers;
				if (allMembers != null)
				{
					T[] array = allMembers;
					foreach (T val in array)
					{
						if ((object)val == null)
						{
							break;
						}
						if (val is RtFieldInfo rtFieldInfo && rtFieldInfo.GetFieldHandle() == field.Value)
						{
							return rtFieldInfo;
						}
					}
				}
				FieldAttributes attributes = RuntimeFieldHandle.GetAttributes(field);
				bool isPublic = (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
				bool isStatic = (attributes & FieldAttributes.Static) != 0;
				RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(field);
				bool isInherited = (RuntimeFieldHandle.AcquiresContextFromThis(field) ? (!RuntimeTypeHandle.CompareCanonicalHandles(approxDeclaringType, ReflectedType)) : (approxDeclaringType != ReflectedType));
				BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
				T[] list = (T[])(object)new RuntimeFieldInfo[1]
				{
					new RtFieldInfo(field, ReflectedType, m_runtimeTypeCache, bindingFlags)
				};
				Insert(ref list, null, MemberListType.HandleToInfo);
				return (FieldInfo)(object)list[0];
			}

			private unsafe T[] Populate(string name, MemberListType listType, CacheType cacheType)
			{
				T[] list;
				if (string.IsNullOrEmpty(name) || (cacheType == CacheType.Constructor && name[0] != '.' && name[0] != '*'))
				{
					list = GetListByName(null, 0, null, 0, listType, cacheType);
				}
				else
				{
					int length = name.Length;
					fixed (char* ptr = name)
					{
						int byteCount = Encoding.UTF8.GetByteCount(ptr, length);
						if (byteCount > 1024)
						{
							byte[] array = new byte[byteCount];
							fixed (byte* pUtf8Name = &array[0])
							{
								list = GetListByName(ptr, length, pUtf8Name, byteCount, listType, cacheType);
							}
						}
						else
						{
							byte* pUtf8Name2 = stackalloc byte[(int)(uint)byteCount];
							list = GetListByName(ptr, length, pUtf8Name2, byteCount, listType, cacheType);
						}
					}
				}
				Insert(ref list, name, listType);
				return list;
			}

			private unsafe T[] GetListByName(char* pName, int cNameLen, byte* pUtf8Name, int cUtf8Name, MemberListType listType, CacheType cacheType)
			{
				if (cNameLen != 0)
				{
					Encoding.UTF8.GetBytes(pName, cNameLen, pUtf8Name, cUtf8Name);
				}
				Filter filter = new Filter(pUtf8Name, cUtf8Name, listType);
				object obj = null;
				switch (cacheType)
				{
				case CacheType.Method:
					obj = PopulateMethods(filter);
					break;
				case CacheType.Field:
					obj = PopulateFields(filter);
					break;
				case CacheType.Constructor:
					obj = PopulateConstructors(filter);
					break;
				case CacheType.Property:
					obj = PopulateProperties(filter);
					break;
				case CacheType.Event:
					obj = PopulateEvents(filter);
					break;
				case CacheType.NestedType:
					obj = PopulateNestedClasses(filter);
					break;
				case CacheType.Interface:
					obj = PopulateInterfaces(filter);
					break;
				}
				return (T[])obj;
			}

			internal void Insert(ref T[] list, string name, MemberListType listType)
			{
				bool lockTaken = false;
				try
				{
					Monitor.Enter(this, ref lockTaken);
					switch (listType)
					{
					case MemberListType.CaseSensitive:
					{
						T[] array = m_csMemberInfos[name];
						if (array == null)
						{
							MergeWithGlobalList(list);
							m_csMemberInfos[name] = list;
						}
						else
						{
							list = array;
						}
						break;
					}
					case MemberListType.CaseInsensitive:
					{
						T[] array2 = m_cisMemberInfos[name];
						if (array2 == null)
						{
							MergeWithGlobalList(list);
							m_cisMemberInfos[name] = list;
						}
						else
						{
							list = array2;
						}
						break;
					}
					case MemberListType.All:
						if (!m_cacheComplete)
						{
							MergeWithGlobalListInOrder(list);
							int num = m_allMembers.Length;
							while (num > 0 && !(m_allMembers[num - 1] != null))
							{
								num--;
							}
							Array.Resize(ref m_allMembers, num);
							Volatile.Write(ref m_cacheComplete, value: true);
						}
						list = m_allMembers;
						break;
					default:
						MergeWithGlobalList(list);
						break;
					}
				}
				finally
				{
					if (lockTaken)
					{
						Monitor.Exit(this);
					}
				}
			}

			private void MergeWithGlobalListInOrder(T[] list)
			{
				T[] allMembers = m_allMembers;
				if (allMembers == null)
				{
					m_allMembers = list;
					return;
				}
				T[] array = allMembers;
				foreach (T val in array)
				{
					if (val == null)
					{
						break;
					}
					for (int j = 0; j < list.Length; j++)
					{
						T val2 = list[j];
						if (val2.CacheEquals(val))
						{
							list[j] = val;
							break;
						}
					}
				}
				m_allMembers = list;
			}

			private void MergeWithGlobalList(T[] list)
			{
				T[] array = m_allMembers;
				if (array == null)
				{
					m_allMembers = list;
					return;
				}
				int num = array.Length;
				int num2 = 0;
				for (int i = 0; i < list.Length; i++)
				{
					T val = list[i];
					bool flag = false;
					int j;
					for (j = 0; j < num; j++)
					{
						T val2 = array[j];
						if (val2 == null)
						{
							break;
						}
						if (val.CacheEquals(val2))
						{
							list[i] = val2;
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						if (num2 == 0)
						{
							num2 = j;
						}
						if (num2 >= array.Length)
						{
							int newSize = ((!m_cacheComplete) ? Math.Max(Math.Max(4, 2 * array.Length), list.Length) : (array.Length + 1));
							T[] array2 = array;
							Array.Resize(ref array2, newSize);
							array = array2;
						}
						Volatile.Write(ref array[num2], val);
						num2++;
					}
				}
				m_allMembers = array;
			}

			private unsafe RuntimeMethodInfo[] PopulateMethods(Filter filter)
			{
				ListBuilder<RuntimeMethodInfo> listBuilder = default(ListBuilder<RuntimeMethodInfo>);
				RuntimeType runtimeType = ReflectedType;
				if (RuntimeTypeHandle.IsInterface(runtimeType))
				{
					RuntimeTypeHandle.IntroducedMethodEnumerator enumerator = RuntimeTypeHandle.GetIntroducedMethods(runtimeType).GetEnumerator();
					while (enumerator.MoveNext())
					{
						RuntimeMethodHandleInternal current = enumerator.Current;
						if (!filter.RequiresStringComparison() || filter.Match(RuntimeMethodHandle.GetUtf8Name(current)))
						{
							MethodAttributes attributes = RuntimeMethodHandle.GetAttributes(current);
							if ((attributes & MethodAttributes.RTSpecialName) == 0)
							{
								bool isPublic = (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
								bool isStatic = (attributes & MethodAttributes.Static) != 0;
								BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited: false, isStatic);
								RuntimeMethodHandleInternal stubIfNeeded = RuntimeMethodHandle.GetStubIfNeeded(current, runtimeType, null);
								RuntimeMethodInfo item = new RuntimeMethodInfo(stubIfNeeded, runtimeType, m_runtimeTypeCache, attributes, bindingFlags, null);
								listBuilder.Add(item);
							}
						}
					}
				}
				else
				{
					while (RuntimeTypeHandle.IsGenericVariable(runtimeType))
					{
						runtimeType = runtimeType.GetBaseType();
					}
					int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
					bool* ptr = stackalloc bool[(int)(uint)numVirtuals];
					new Span<bool>(ptr, numVirtuals).Clear();
					bool isValueType = runtimeType.IsValueType;
					do
					{
						int numVirtuals2 = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
						RuntimeTypeHandle.IntroducedMethodEnumerator enumerator2 = RuntimeTypeHandle.GetIntroducedMethods(runtimeType).GetEnumerator();
						while (enumerator2.MoveNext())
						{
							RuntimeMethodHandleInternal current2 = enumerator2.Current;
							if (filter.RequiresStringComparison() && !filter.Match(RuntimeMethodHandle.GetUtf8Name(current2)))
							{
								continue;
							}
							MethodAttributes attributes2 = RuntimeMethodHandle.GetAttributes(current2);
							MethodAttributes methodAttributes = attributes2 & MethodAttributes.MemberAccessMask;
							if ((attributes2 & MethodAttributes.RTSpecialName) != 0)
							{
								continue;
							}
							bool flag = false;
							int num = 0;
							if ((attributes2 & MethodAttributes.Virtual) != 0)
							{
								num = RuntimeMethodHandle.GetSlot(current2);
								flag = num < numVirtuals2;
							}
							bool flag2 = runtimeType != ReflectedType;
							bool flag3 = methodAttributes == MethodAttributes.Private;
							if (flag2 && flag3 && !flag)
							{
								continue;
							}
							if (flag)
							{
								if (ptr[num])
								{
									continue;
								}
								ptr[num] = true;
							}
							else if (isValueType && (attributes2 & (MethodAttributes.Virtual | MethodAttributes.Abstract)) != 0)
							{
								continue;
							}
							bool isPublic2 = methodAttributes == MethodAttributes.Public;
							bool isStatic2 = (attributes2 & MethodAttributes.Static) != 0;
							BindingFlags bindingFlags2 = FilterPreCalculate(isPublic2, flag2, isStatic2);
							RuntimeMethodHandleInternal stubIfNeeded2 = RuntimeMethodHandle.GetStubIfNeeded(current2, runtimeType, null);
							RuntimeMethodInfo item2 = new RuntimeMethodInfo(stubIfNeeded2, runtimeType, m_runtimeTypeCache, attributes2, bindingFlags2, null);
							listBuilder.Add(item2);
						}
						runtimeType = RuntimeTypeHandle.GetBaseType(runtimeType);
					}
					while (runtimeType != null);
				}
				return listBuilder.ToArray();
			}

			private RuntimeConstructorInfo[] PopulateConstructors(Filter filter)
			{
				if (ReflectedType.IsGenericParameter)
				{
					return Array.Empty<RuntimeConstructorInfo>();
				}
				ListBuilder<RuntimeConstructorInfo> listBuilder = default(ListBuilder<RuntimeConstructorInfo>);
				RuntimeType reflectedType = ReflectedType;
				RuntimeTypeHandle.IntroducedMethodEnumerator enumerator = RuntimeTypeHandle.GetIntroducedMethods(reflectedType).GetEnumerator();
				while (enumerator.MoveNext())
				{
					RuntimeMethodHandleInternal current = enumerator.Current;
					if (!filter.RequiresStringComparison() || filter.Match(RuntimeMethodHandle.GetUtf8Name(current)))
					{
						MethodAttributes attributes = RuntimeMethodHandle.GetAttributes(current);
						if ((attributes & MethodAttributes.RTSpecialName) != 0)
						{
							bool isPublic = (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
							bool isStatic = (attributes & MethodAttributes.Static) != 0;
							BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited: false, isStatic);
							RuntimeMethodHandleInternal stubIfNeeded = RuntimeMethodHandle.GetStubIfNeeded(current, reflectedType, null);
							RuntimeConstructorInfo item = new RuntimeConstructorInfo(stubIfNeeded, ReflectedType, m_runtimeTypeCache, attributes, bindingFlags);
							listBuilder.Add(item);
						}
					}
				}
				return listBuilder.ToArray();
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "Calls to GetInterfaces technically require all interfaces on ReflectedTypeBut this is not a public API to enumerate reflection items, all the public APIs which do thatshould be annotated accordingly.")]
			private RuntimeFieldInfo[] PopulateFields(Filter filter)
			{
				ListBuilder<RuntimeFieldInfo> list = default(ListBuilder<RuntimeFieldInfo>);
				RuntimeType runtimeType = ReflectedType;
				while (RuntimeTypeHandle.IsGenericVariable(runtimeType))
				{
					runtimeType = runtimeType.GetBaseType();
				}
				while (runtimeType != null)
				{
					PopulateRtFields(filter, runtimeType, ref list);
					PopulateLiteralFields(filter, runtimeType, ref list);
					runtimeType = RuntimeTypeHandle.GetBaseType(runtimeType);
				}
				if (ReflectedType.IsGenericParameter)
				{
					Type[] interfaces = ReflectedType.BaseType.GetInterfaces();
					for (int i = 0; i < interfaces.Length; i++)
					{
						PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
						PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
					}
				}
				else
				{
					Type[] interfaces2 = RuntimeTypeHandle.GetInterfaces(ReflectedType);
					if (interfaces2 != null)
					{
						for (int j = 0; j < interfaces2.Length; j++)
						{
							PopulateLiteralFields(filter, (RuntimeType)interfaces2[j], ref list);
							PopulateRtFields(filter, (RuntimeType)interfaces2[j], ref list);
						}
					}
				}
				return list.ToArray();
			}

			private unsafe void PopulateRtFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
			{
				nint* ptr = (nint*)stackalloc byte[(int)checked((nuint)64u * (nuint)8u)];
				int num = 64;
				if (!RuntimeTypeHandle.GetFields(declaringType, ptr, &num))
				{
					fixed (nint* ptr2 = new nint[num])
					{
						RuntimeTypeHandle.GetFields(declaringType, ptr2, &num);
						PopulateRtFields(filter, ptr2, num, declaringType, ref list);
					}
				}
				else if (num > 0)
				{
					PopulateRtFields(filter, ptr, num, declaringType, ref list);
				}
			}

			private unsafe void PopulateRtFields(Filter filter, nint* ppFieldHandles, int count, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
			{
				bool flag = declaringType.IsGenericType && !RuntimeTypeHandle.ContainsGenericVariables(declaringType);
				bool flag2 = declaringType != ReflectedType;
				for (int i = 0; i < count; i++)
				{
					RuntimeFieldHandleInternal runtimeFieldHandleInternal = new RuntimeFieldHandleInternal(*(nint*)((byte*)ppFieldHandles + (nint)i * (nint)8));
					if (filter.RequiresStringComparison() && !filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandleInternal)))
					{
						continue;
					}
					FieldAttributes attributes = RuntimeFieldHandle.GetAttributes(runtimeFieldHandleInternal);
					FieldAttributes fieldAttributes = attributes & FieldAttributes.FieldAccessMask;
					if (!flag2 || fieldAttributes != FieldAttributes.Private)
					{
						bool isPublic = fieldAttributes == FieldAttributes.Public;
						bool flag3 = (attributes & FieldAttributes.Static) != 0;
						BindingFlags bindingFlags = FilterPreCalculate(isPublic, flag2, flag3);
						if (flag && flag3)
						{
							runtimeFieldHandleInternal = RuntimeFieldHandle.GetStaticFieldForGenericType(runtimeFieldHandleInternal, declaringType);
						}
						RuntimeFieldInfo item = new RtFieldInfo(runtimeFieldHandleInternal, declaringType, m_runtimeTypeCache, bindingFlags);
						list.Add(item);
					}
				}
			}

			private void PopulateLiteralFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
			{
				int token = RuntimeTypeHandle.GetToken(declaringType);
				if (System.Reflection.MetadataToken.IsNullToken(token))
				{
					return;
				}
				MetadataImport metadataImport = RuntimeTypeHandle.GetMetadataImport(declaringType);
				metadataImport.EnumFields(token, out var result);
				for (int i = 0; i < result.Length; i++)
				{
					int num = result[i];
					metadataImport.GetFieldDefProps(num, out var fieldAttributes);
					FieldAttributes fieldAttributes2 = fieldAttributes & FieldAttributes.FieldAccessMask;
					if ((fieldAttributes & FieldAttributes.Literal) == 0)
					{
						continue;
					}
					bool flag = declaringType != ReflectedType;
					if (flag && fieldAttributes2 == FieldAttributes.Private)
					{
						continue;
					}
					if (filter.RequiresStringComparison())
					{
						MdUtf8String name = metadataImport.GetName(num);
						if (!filter.Match(name))
						{
							continue;
						}
					}
					bool isPublic = fieldAttributes2 == FieldAttributes.Public;
					bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
					BindingFlags bindingFlags = FilterPreCalculate(isPublic, flag, isStatic);
					RuntimeFieldInfo item = new MdFieldInfo(num, fieldAttributes, declaringType.TypeHandle, m_runtimeTypeCache, bindingFlags);
					list.Add(item);
				}
			}

			private void AddSpecialInterface(ref ListBuilder<RuntimeType> list, Filter filter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] RuntimeType iList, bool addSubInterface)
			{
				if (!iList.IsAssignableFrom(ReflectedType))
				{
					return;
				}
				if (filter.Match(RuntimeTypeHandle.GetUtf8Name(iList)))
				{
					list.Add(iList);
				}
				if (!addSubInterface)
				{
					return;
				}
				Type[] interfaces = iList.GetInterfaces();
				for (int i = 0; i < interfaces.Length; i++)
				{
					RuntimeType runtimeType = (RuntimeType)interfaces[i];
					if (runtimeType.IsGenericType && filter.Match(RuntimeTypeHandle.GetUtf8Name(runtimeType)))
					{
						list.Add(runtimeType);
					}
				}
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2065:UnrecognizedReflectionPattern", Justification = "Calls to GetInterfaces technically require all interfaces on ReflectedTypeBut this is not a public API to enumerate reflection items, all the public APIs which do thatshould be annotated accordingly.")]
			private RuntimeType[] PopulateInterfaces(Filter filter)
			{
				ListBuilder<RuntimeType> list = default(ListBuilder<RuntimeType>);
				RuntimeType reflectedType = ReflectedType;
				if (!RuntimeTypeHandle.IsGenericVariable(reflectedType))
				{
					Type[] interfaces = RuntimeTypeHandle.GetInterfaces(reflectedType);
					if (interfaces != null)
					{
						for (int i = 0; i < interfaces.Length; i++)
						{
							RuntimeType runtimeType = (RuntimeType)interfaces[i];
							if (!filter.RequiresStringComparison() || filter.Match(RuntimeTypeHandle.GetUtf8Name(runtimeType)))
							{
								list.Add(runtimeType);
							}
						}
					}
					if (ReflectedType.IsSZArray)
					{
						RuntimeType runtimeType2 = (RuntimeType)ReflectedType.GetElementType();
						if (!runtimeType2.IsPointer)
						{
							AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IList<>).MakeGenericType(runtimeType2), addSubInterface: true);
							AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IReadOnlyList<>).MakeGenericType(runtimeType2), addSubInterface: false);
							AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IReadOnlyCollection<>).MakeGenericType(runtimeType2), addSubInterface: false);
						}
					}
				}
				else
				{
					HashSet<RuntimeType> hashSet = new HashSet<RuntimeType>();
					Type[] genericParameterConstraints = reflectedType.GetGenericParameterConstraints();
					for (int j = 0; j < genericParameterConstraints.Length; j++)
					{
						RuntimeType runtimeType3 = (RuntimeType)genericParameterConstraints[j];
						if (runtimeType3.IsInterface)
						{
							hashSet.Add(runtimeType3);
						}
						Type[] interfaces2 = runtimeType3.GetInterfaces();
						for (int k = 0; k < interfaces2.Length; k++)
						{
							hashSet.Add((RuntimeType)interfaces2[k]);
						}
					}
					foreach (RuntimeType item in hashSet)
					{
						if (!filter.RequiresStringComparison() || filter.Match(RuntimeTypeHandle.GetUtf8Name(item)))
						{
							list.Add(item);
						}
					}
				}
				return list.ToArray();
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Calls to ResolveTypeHandle technically require all types to be kept But this is not a public API to enumerate reflection items, all the public APIs which do that should be annotated accordingly.")]
			private RuntimeType[] PopulateNestedClasses(Filter filter)
			{
				RuntimeType runtimeType = ReflectedType;
				while (RuntimeTypeHandle.IsGenericVariable(runtimeType))
				{
					runtimeType = runtimeType.GetBaseType();
				}
				int token = RuntimeTypeHandle.GetToken(runtimeType);
				if (System.Reflection.MetadataToken.IsNullToken(token))
				{
					return Array.Empty<RuntimeType>();
				}
				ListBuilder<RuntimeType> listBuilder = default(ListBuilder<RuntimeType>);
				ModuleHandle moduleHandle = new ModuleHandle(RuntimeTypeHandle.GetModule(runtimeType));
				ModuleHandle.GetMetadataImport(moduleHandle.GetRuntimeModule()).EnumNestedTypes(token, out var result);
				for (int i = 0; i < result.Length; i++)
				{
					RuntimeType runtimeType2;
					try
					{
						runtimeType2 = moduleHandle.ResolveTypeHandle(result[i]).GetRuntimeType();
					}
					catch (TypeLoadException)
					{
						continue;
					}
					if (!filter.RequiresStringComparison() || filter.Match(RuntimeTypeHandle.GetUtf8Name(runtimeType2)))
					{
						listBuilder.Add(runtimeType2);
					}
				}
				return listBuilder.ToArray();
			}

			private RuntimeEventInfo[] PopulateEvents(Filter filter)
			{
				Dictionary<string, RuntimeEventInfo> csEventInfos = (filter.CaseSensitive() ? null : new Dictionary<string, RuntimeEventInfo>());
				RuntimeType runtimeType = ReflectedType;
				ListBuilder<RuntimeEventInfo> list = default(ListBuilder<RuntimeEventInfo>);
				if (!RuntimeTypeHandle.IsInterface(runtimeType))
				{
					while (RuntimeTypeHandle.IsGenericVariable(runtimeType))
					{
						runtimeType = runtimeType.GetBaseType();
					}
					while (runtimeType != null)
					{
						PopulateEvents(filter, runtimeType, csEventInfos, ref list);
						runtimeType = RuntimeTypeHandle.GetBaseType(runtimeType);
					}
				}
				else
				{
					PopulateEvents(filter, runtimeType, csEventInfos, ref list);
				}
				return list.ToArray();
			}

			private void PopulateEvents(Filter filter, RuntimeType declaringType, Dictionary<string, RuntimeEventInfo> csEventInfos, ref ListBuilder<RuntimeEventInfo> list)
			{
				int token = RuntimeTypeHandle.GetToken(declaringType);
				if (System.Reflection.MetadataToken.IsNullToken(token))
				{
					return;
				}
				MetadataImport metadataImport = RuntimeTypeHandle.GetMetadataImport(declaringType);
				metadataImport.EnumEvents(token, out var result);
				for (int i = 0; i < result.Length; i++)
				{
					int num = result[i];
					if (filter.RequiresStringComparison())
					{
						MdUtf8String name = metadataImport.GetName(num);
						if (!filter.Match(name))
						{
							continue;
						}
					}
					bool isPrivate;
					RuntimeEventInfo runtimeEventInfo = new RuntimeEventInfo(num, declaringType, m_runtimeTypeCache, out isPrivate);
					if (declaringType != m_runtimeTypeCache.GetRuntimeType() && isPrivate)
					{
						continue;
					}
					if (csEventInfos != null)
					{
						string name2 = runtimeEventInfo.Name;
						if (csEventInfos.ContainsKey(name2))
						{
							continue;
						}
						csEventInfos[name2] = runtimeEventInfo;
					}
					else if (list.Count > 0)
					{
						break;
					}
					list.Add(runtimeEventInfo);
				}
			}

			private RuntimePropertyInfo[] PopulateProperties(Filter filter)
			{
				RuntimeType runtimeType = ReflectedType;
				ListBuilder<RuntimePropertyInfo> list = default(ListBuilder<RuntimePropertyInfo>);
				if (!RuntimeTypeHandle.IsInterface(runtimeType))
				{
					while (RuntimeTypeHandle.IsGenericVariable(runtimeType))
					{
						runtimeType = runtimeType.GetBaseType();
					}
					Dictionary<string, List<RuntimePropertyInfo>> csPropertyInfos = (filter.CaseSensitive() ? null : new Dictionary<string, List<RuntimePropertyInfo>>());
					int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
					Span<bool> usedSlots;
					if (numVirtuals <= 128)
					{
						usedSlots = stackalloc bool[numVirtuals];
						usedSlots.Clear();
					}
					else
					{
						usedSlots = new bool[numVirtuals];
					}
					do
					{
						PopulateProperties(filter, runtimeType, csPropertyInfos, usedSlots, isInterface: false, ref list);
						runtimeType = RuntimeTypeHandle.GetBaseType(runtimeType);
					}
					while (runtimeType != null);
				}
				else
				{
					PopulateProperties(filter, runtimeType, null, default(Span<bool>), isInterface: true, ref list);
				}
				return list.ToArray();
			}

			private void PopulateProperties(Filter filter, RuntimeType declaringType, Dictionary<string, List<RuntimePropertyInfo>> csPropertyInfos, Span<bool> usedSlots, bool isInterface, ref ListBuilder<RuntimePropertyInfo> list)
			{
				int token = RuntimeTypeHandle.GetToken(declaringType);
				if (System.Reflection.MetadataToken.IsNullToken(token))
				{
					return;
				}
				RuntimeTypeHandle.GetMetadataImport(declaringType).EnumProperties(token, out var result);
				int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(declaringType);
				for (int i = 0; i < result.Length; i++)
				{
					int num = result[i];
					if (filter.RequiresStringComparison())
					{
						MdUtf8String name = declaringType.GetRuntimeModule().MetadataImport.GetName(num);
						if (!filter.Match(name))
						{
							continue;
						}
					}
					bool isPrivate;
					RuntimePropertyInfo runtimePropertyInfo = new RuntimePropertyInfo(num, declaringType, m_runtimeTypeCache, out isPrivate);
					if (!isInterface)
					{
						if (declaringType != ReflectedType && isPrivate)
						{
							continue;
						}
						MethodInfo methodInfo = runtimePropertyInfo.GetGetMethod() ?? runtimePropertyInfo.GetSetMethod();
						if (methodInfo != null)
						{
							int slot = RuntimeMethodHandle.GetSlot((RuntimeMethodInfo)methodInfo);
							if (slot < numVirtuals)
							{
								if (usedSlots[slot])
								{
									continue;
								}
								usedSlots[slot] = true;
							}
						}
						if (csPropertyInfos != null)
						{
							string name2 = runtimePropertyInfo.Name;
							if (!csPropertyInfos.TryGetValue(name2, out var value))
							{
								value = (csPropertyInfos[name2] = new List<RuntimePropertyInfo>(1));
							}
							for (int j = 0; j < value.Count; j++)
							{
								if (runtimePropertyInfo.EqualsSig(value[j]))
								{
									value = null;
									break;
								}
							}
							if (value == null)
							{
								continue;
							}
							value.Add(runtimePropertyInfo);
						}
						else
						{
							bool flag = false;
							for (int k = 0; k < list.Count; k++)
							{
								if (runtimePropertyInfo.EqualsSig(list[k]))
								{
									flag = true;
									break;
								}
							}
							if (flag)
							{
								continue;
							}
						}
					}
					list.Add(runtimePropertyInfo);
				}
			}

			internal T[] GetMemberList(MemberListType listType, string name, CacheType cacheType)
			{
				switch (listType)
				{
				case MemberListType.CaseSensitive:
					return m_csMemberInfos[name] ?? Populate(name, listType, cacheType);
				case MemberListType.CaseInsensitive:
					return m_cisMemberInfos[name] ?? Populate(name, listType, cacheType);
				default:
					if (Volatile.Read(ref m_cacheComplete))
					{
						return m_allMembers;
					}
					return Populate(null, listType, cacheType);
				}
			}
		}

		private readonly RuntimeType m_runtimeType;

		private RuntimeType m_enclosingType;

		private TypeCode m_typeCode;

		private string m_name;

		private string m_fullname;

		private string m_toString;

		private string m_namespace;

		private readonly bool m_isGlobal;

		private bool m_bIsDomainInitialized;

		private MemberInfoCache<RuntimeMethodInfo> m_methodInfoCache;

		private MemberInfoCache<RuntimeConstructorInfo> m_constructorInfoCache;

		private MemberInfoCache<RuntimeFieldInfo> m_fieldInfoCache;

		private MemberInfoCache<RuntimeType> m_interfaceCache;

		private MemberInfoCache<RuntimeType> m_nestedClassesCache;

		private MemberInfoCache<RuntimePropertyInfo> m_propertyInfoCache;

		private MemberInfoCache<RuntimeEventInfo> m_eventInfoCache;

		private static CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> s_methodInstantiations;

		private static object s_methodInstantiationsLock;

		private string m_defaultMemberName;

		private object m_genericCache;

		private object[] _emptyArray;

		private RuntimeType _genericTypeDefinition;

		internal object GenericCache
		{
			get
			{
				return m_genericCache;
			}
			set
			{
				m_genericCache = value;
			}
		}

		internal Type[] FunctionPointerReturnAndParameterTypes
		{
			get
			{
				Type[] array = (Type[])GenericCache;
				if (array == null)
				{
					array = (Type[])(GenericCache = RuntimeTypeHandle.GetArgumentTypesFromFunctionPointer(m_runtimeType));
				}
				return array;
			}
		}

		internal bool DomainInitialized
		{
			get
			{
				return m_bIsDomainInitialized;
			}
			set
			{
				m_bIsDomainInitialized = value;
			}
		}

		internal TypeCode TypeCode
		{
			get
			{
				return m_typeCode;
			}
			set
			{
				m_typeCode = value;
			}
		}

		internal bool IsGlobal => m_isGlobal;

		internal RuntimeTypeCache(RuntimeType runtimeType)
		{
			m_typeCode = TypeCode.Empty;
			m_runtimeType = runtimeType;
			m_isGlobal = RuntimeTypeHandle.GetModule(runtimeType).RuntimeType == runtimeType;
		}

		private string ConstructName([NotNull] ref string name, TypeNameFormatFlags formatFlags)
		{
			return name ?? (name = new RuntimeTypeHandle(m_runtimeType).ConstructName(formatFlags));
		}

		private T[] GetMemberList<T>(ref MemberInfoCache<T> m_cache, MemberListType listType, string name, CacheType cacheType) where T : MemberInfo
		{
			MemberInfoCache<T> memberCache = GetMemberCache(ref m_cache);
			return memberCache.GetMemberList(listType, name, cacheType);
		}

		private MemberInfoCache<T> GetMemberCache<T>(ref MemberInfoCache<T> m_cache) where T : MemberInfo
		{
			MemberInfoCache<T> memberInfoCache = m_cache;
			if (memberInfoCache == null)
			{
				MemberInfoCache<T> memberInfoCache2 = new MemberInfoCache<T>(this);
				memberInfoCache = Interlocked.CompareExchange(ref m_cache, memberInfoCache2, null);
				if (memberInfoCache == null)
				{
					memberInfoCache = memberInfoCache2;
				}
			}
			return memberInfoCache;
		}

		internal string GetName(TypeNameKind kind)
		{
			switch (kind)
			{
			case TypeNameKind.Name:
				return ConstructName(ref m_name, TypeNameFormatFlags.FormatBasic);
			case TypeNameKind.FullName:
				if (!m_runtimeType.GetRootElementType().IsGenericTypeDefinition && m_runtimeType.ContainsGenericParameters)
				{
					return null;
				}
				if (m_runtimeType.IsFunctionPointer)
				{
					return null;
				}
				return ConstructName(ref m_fullname, (TypeNameFormatFlags)3);
			case TypeNameKind.ToString:
				return ConstructName(ref m_toString, TypeNameFormatFlags.FormatNamespace);
			default:
				throw new InvalidOperationException();
			}
		}

		internal string GetNameSpace()
		{
			if (m_namespace == null)
			{
				Type runtimeType = m_runtimeType;
				if (runtimeType.IsFunctionPointer)
				{
					return null;
				}
				runtimeType = runtimeType.GetRootElementType();
				while (runtimeType.IsNested)
				{
					runtimeType = runtimeType.DeclaringType;
				}
				m_namespace = RuntimeTypeHandle.GetMetadataImport((RuntimeType)runtimeType).GetNamespace(runtimeType.MetadataToken).ToString();
			}
			return m_namespace;
		}

		internal RuntimeType GetEnclosingType()
		{
			if (m_enclosingType == null)
			{
				RuntimeType declaringType = RuntimeTypeHandle.GetDeclaringType(GetRuntimeType());
				m_enclosingType = declaringType ?? ((RuntimeType)typeof(void));
			}
			if (!(m_enclosingType == typeof(void)))
			{
				return m_enclosingType;
			}
			return null;
		}

		internal RuntimeType GetRuntimeType()
		{
			return m_runtimeType;
		}

		internal void InvalidateCachedNestedType()
		{
			m_nestedClassesCache = null;
		}

		internal string GetDefaultMemberName()
		{
			if (m_defaultMemberName == null)
			{
				CustomAttributeData customAttributeData = null;
				Type typeFromHandle = typeof(DefaultMemberAttribute);
				RuntimeType runtimeType = m_runtimeType;
				while (runtimeType != null)
				{
					IList<CustomAttributeData> customAttributes = CustomAttributeData.GetCustomAttributes(runtimeType);
					for (int i = 0; i < customAttributes.Count; i++)
					{
						if ((object)customAttributes[i].Constructor.DeclaringType == typeFromHandle)
						{
							customAttributeData = customAttributes[i];
							break;
						}
					}
					if (customAttributeData != null)
					{
						m_defaultMemberName = customAttributeData.ConstructorArguments[0].Value as string;
						break;
					}
					runtimeType = runtimeType.GetBaseType();
				}
			}
			return m_defaultMemberName;
		}

		internal object[] GetEmptyArray()
		{
			return _emptyArray ?? (_emptyArray = (object[])Array.CreateInstance(m_runtimeType, 0));
		}

		internal RuntimeType GetGenericTypeDefinition()
		{
			return _genericTypeDefinition ?? CacheGenericDefinition();
			[MethodImpl(MethodImplOptions.NoInlining)]
			RuntimeType CacheGenericDefinition()
			{
				RuntimeType o = null;
				if (m_runtimeType.IsGenericTypeDefinition)
				{
					o = m_runtimeType;
				}
				else
				{
					RuntimeType type = m_runtimeType;
					RuntimeTypeHandle.GetGenericTypeDefinition(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref o));
				}
				return _genericTypeDefinition = o;
			}
		}

		internal MethodInfo GetGenericMethodInfo(RuntimeMethodHandleInternal genericMethod)
		{
			LoaderAllocator loaderAllocator = RuntimeMethodHandle.GetLoaderAllocator(genericMethod);
			RuntimeMethodInfo runtimeMethodInfo = new RuntimeMethodInfo(genericMethod, RuntimeMethodHandle.GetDeclaringType(genericMethod), this, RuntimeMethodHandle.GetAttributes(genericMethod), (BindingFlags)(-1), loaderAllocator);
			RuntimeMethodInfo runtimeMethodInfo2 = ((loaderAllocator == null) ? s_methodInstantiations[runtimeMethodInfo] : loaderAllocator.m_methodInstantiations[runtimeMethodInfo]);
			if (runtimeMethodInfo2 != null)
			{
				return runtimeMethodInfo2;
			}
			if (s_methodInstantiationsLock == null)
			{
				Interlocked.CompareExchange(ref s_methodInstantiationsLock, new object(), null);
			}
			bool lockTaken = false;
			try
			{
				Monitor.Enter(s_methodInstantiationsLock, ref lockTaken);
				if (loaderAllocator != null)
				{
					runtimeMethodInfo2 = loaderAllocator.m_methodInstantiations[runtimeMethodInfo];
					if (runtimeMethodInfo2 != null)
					{
						return runtimeMethodInfo2;
					}
					loaderAllocator.m_methodInstantiations[runtimeMethodInfo] = runtimeMethodInfo;
				}
				else
				{
					runtimeMethodInfo2 = s_methodInstantiations[runtimeMethodInfo];
					if (runtimeMethodInfo2 != null)
					{
						return runtimeMethodInfo2;
					}
					s_methodInstantiations[runtimeMethodInfo] = runtimeMethodInfo;
				}
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(s_methodInstantiationsLock);
				}
			}
			return runtimeMethodInfo;
		}

		internal RuntimeMethodInfo[] GetMethodList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_methodInfoCache, listType, name, CacheType.Method);
		}

		internal RuntimeConstructorInfo[] GetConstructorList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_constructorInfoCache, listType, name, CacheType.Constructor);
		}

		internal RuntimePropertyInfo[] GetPropertyList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_propertyInfoCache, listType, name, CacheType.Property);
		}

		internal RuntimeEventInfo[] GetEventList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_eventInfoCache, listType, name, CacheType.Event);
		}

		internal RuntimeFieldInfo[] GetFieldList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_fieldInfoCache, listType, name, CacheType.Field);
		}

		internal RuntimeType[] GetInterfaceList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_interfaceCache, listType, name, CacheType.Interface);
		}

		internal RuntimeType[] GetNestedTypeList(MemberListType listType, string name)
		{
			return GetMemberList(ref m_nestedClassesCache, listType, name, CacheType.NestedType);
		}

		internal MethodBase GetMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method)
		{
			GetMemberCache(ref m_methodInfoCache);
			return m_methodInfoCache.AddMethod(declaringType, method, CacheType.Method);
		}

		internal MethodBase GetConstructor(RuntimeType declaringType, RuntimeMethodHandleInternal constructor)
		{
			GetMemberCache(ref m_constructorInfoCache);
			return m_constructorInfoCache.AddMethod(declaringType, constructor, CacheType.Constructor);
		}

		internal FieldInfo GetField(RuntimeFieldHandleInternal field)
		{
			GetMemberCache(ref m_fieldInfoCache);
			return m_fieldInfoCache.AddField(field);
		}
	}

	[Flags]
	private enum DispatchWrapperType
	{
		Unknown = 1,
		Dispatch = 2,
		Error = 8,
		Currency = 0x10,
		BStr = 0x20,
		SafeArray = 0x10000
	}

	private enum CheckValueStatus
	{
		Success,
		ArgumentException,
		NotSupported_ByRefLike
	}

	private readonly object m_keepalive;

	private nint m_cache;

	internal nint m_handle;

	internal static readonly RuntimeType ValueType = (RuntimeType)typeof(ValueType);

	private static readonly RuntimeType ObjectType = (RuntimeType)typeof(object);

	private static readonly RuntimeType StringType = (RuntimeType)typeof(string);

	private const int GenericParameterCountAny = -1;

	private static OleAutBinder s_ForwardCallBinder;

	internal object GenericCache
	{
		get
		{
			return CacheIfExists?.GenericCache;
		}
		set
		{
			Cache.GenericCache = value;
		}
	}

	internal bool DomainInitialized
	{
		get
		{
			return Cache.DomainInitialized;
		}
		set
		{
			Cache.DomainInitialized = value;
		}
	}

	private RuntimeTypeCache CacheIfExists
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (m_cache != IntPtr.Zero)
			{
				object o = GCHandle.InternalGet(m_cache);
				return Unsafe.As<RuntimeTypeCache>(o);
			}
			return null;
		}
	}

	private RuntimeTypeCache Cache
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (m_cache != IntPtr.Zero)
			{
				object obj = GCHandle.InternalGet(m_cache);
				if (obj != null)
				{
					return Unsafe.As<RuntimeTypeCache>(obj);
				}
			}
			return InitializeCache();
		}
	}

	public sealed override bool IsCollectible
	{
		get
		{
			RuntimeType type = this;
			return RuntimeTypeHandle.IsCollectible(new QCallTypeHandle(ref type)) != Interop.BOOL.FALSE;
		}
	}

	public override MethodBase DeclaringMethod
	{
		get
		{
			if (!IsGenericParameter)
			{
				throw new InvalidOperationException(SR.Arg_NotGenericParameter);
			}
			IRuntimeMethodInfo declaringMethod = RuntimeTypeHandle.GetDeclaringMethod(this);
			if (declaringMethod == null)
			{
				return null;
			}
			return GetMethodBase(RuntimeMethodHandle.GetDeclaringType(declaringMethod), declaringMethod);
		}
	}

	public override string FullName => GetCachedName(TypeNameKind.FullName);

	public override string AssemblyQualifiedName
	{
		get
		{
			string fullName = FullName;
			if (fullName == null)
			{
				return null;
			}
			return Assembly.CreateQualifiedName(Assembly.FullName, fullName);
		}
	}

	public override string Namespace
	{
		get
		{
			string nameSpace = Cache.GetNameSpace();
			if (string.IsNullOrEmpty(nameSpace))
			{
				return null;
			}
			return nameSpace;
		}
	}

	public override Guid GUID
	{
		get
		{
			Guid result = default(Guid);
			GetGUID(ref result);
			return result;
		}
	}

	public unsafe override bool IsEnum
	{
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			if (nativeTypeHandle.IsTypeDesc)
			{
				return IsSubclassOf(typeof(Enum));
			}
			bool result = nativeTypeHandle.AsMethodTable()->ParentMethodTable == System.Runtime.CompilerServices.TypeHandle.TypeHandleOf<Enum>().AsMethodTable();
			GC.KeepAlive(this);
			return result;
		}
	}

	internal unsafe bool IsActualEnum
	{
		[Intrinsic]
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->ParentMethodTable == System.Runtime.CompilerServices.TypeHandle.TypeHandleOf<Enum>().AsMethodTable();
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe override bool IsConstructedGenericType
	{
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->IsConstructedGenericType;
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe override bool IsGenericType
	{
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->HasInstantiation;
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe override bool IsGenericTypeDefinition
	{
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->IsGenericTypeDefinition;
			GC.KeepAlive(this);
			return result;
		}
	}

	public override GenericParameterAttributes GenericParameterAttributes
	{
		get
		{
			if (!IsGenericParameter)
			{
				throw new InvalidOperationException(SR.Arg_NotGenericParameter);
			}
			RuntimeTypeHandle.GetMetadataImport(this).GetGenericParamProps(MetadataToken, out var attributes);
			return attributes;
		}
	}

	public sealed override bool IsSZArray => RuntimeTypeHandle.IsSZArray(this);

	public override int GenericParameterPosition
	{
		get
		{
			if (!IsGenericParameter)
			{
				throw new InvalidOperationException(SR.Arg_NotGenericParameter);
			}
			return new RuntimeTypeHandle(this).GetGenericVariableIndex();
		}
	}

	public override bool ContainsGenericParameters => GetRootElementType().TypeHandle.ContainsGenericVariables();

	internal unsafe bool IsNullableOfT
	{
		get
		{
			TypeHandle nativeTypeHandle = GetNativeTypeHandle();
			bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->IsNullable;
			GC.KeepAlive(this);
			return result;
		}
	}

	public override StructLayoutAttribute StructLayoutAttribute => PseudoCustomAttribute.GetStructLayoutCustomAttribute(this);

	public override bool IsFunctionPointer => RuntimeTypeHandle.IsFunctionPointer(this);

	public override bool IsUnmanagedFunctionPointer => RuntimeTypeHandle.IsUnmanagedFunctionPointer(this);

	public override string Name => GetCachedName(TypeNameKind.Name);

	public override Type DeclaringType => Cache.GetEnclosingType();

	private static OleAutBinder ForwardCallBinder => s_ForwardCallBinder ?? (s_ForwardCallBinder = new OleAutBinder());

	public override Assembly Assembly => RuntimeTypeHandle.GetAssembly(this);

	public override Type BaseType => GetBaseType();

	public override bool IsByRefLike => RuntimeTypeHandle.IsByRefLike(this);

	public override bool IsGenericParameter => RuntimeTypeHandle.IsGenericVariable(this);

	public override bool IsTypeDefinition => RuntimeTypeHandle.IsTypeDefinition(this);

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override MemberTypes MemberType
	{
		get
		{
			if (!base.IsPublic && !base.IsNotPublic)
			{
				return MemberTypes.NestedType;
			}
			return MemberTypes.TypeInfo;
		}
	}

	public override int MetadataToken => RuntimeTypeHandle.GetToken(this);

	public override Module Module => GetRuntimeModule();

	public override Type ReflectedType => DeclaringType;

	public override RuntimeTypeHandle TypeHandle
	{
		[Intrinsic]
		get
		{
			return new RuntimeTypeHandle(this);
		}
	}

	public override Type UnderlyingSystemType => this;

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	internal static MethodBase GetMethodBase(RuntimeModule scope, int typeMetadataToken)
	{
		return GetMethodBase(new ModuleHandle(scope).ResolveMethodHandle(typeMetadataToken).GetMethodInfo());
	}

	internal static MethodBase GetMethodBase(IRuntimeMethodInfo methodHandle)
	{
		return GetMethodBase(null, methodHandle);
	}

	internal static MethodBase GetMethodBase(RuntimeType reflectedType, IRuntimeMethodInfo methodHandle)
	{
		MethodBase methodBase = GetMethodBase(reflectedType, methodHandle.Value);
		GC.KeepAlive(methodHandle);
		return methodBase;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The code in this method looks up the method by name, but it always starts with a method handle.To get here something somewhere had to get the method handle and thus the method must exist.")]
	internal static MethodBase GetMethodBase(RuntimeType reflectedType, RuntimeMethodHandleInternal methodHandle)
	{
		if (RuntimeMethodHandle.IsDynamicMethod(methodHandle))
		{
			return RuntimeMethodHandle.GetResolver(methodHandle)?.GetDynamicMethod();
		}
		RuntimeType runtimeType = RuntimeMethodHandle.GetDeclaringType(methodHandle);
		RuntimeType[] array = null;
		if ((object)reflectedType == null)
		{
			reflectedType = runtimeType;
		}
		if (reflectedType != runtimeType && !reflectedType.IsSubclassOf(runtimeType))
		{
			if (reflectedType.IsArray)
			{
				MethodBase[] array2 = reflectedType.GetMember(RuntimeMethodHandle.GetName(methodHandle), MemberTypes.Constructor | MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) as MethodBase[];
				bool flag = false;
				for (int i = 0; i < array2.Length; i++)
				{
					IRuntimeMethodInfo runtimeMethodInfo = (IRuntimeMethodInfo)array2[i];
					if (runtimeMethodInfo.Value.Value == methodHandle.Value)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethodHandle, reflectedType, runtimeType));
				}
			}
			else if (runtimeType.IsGenericType)
			{
				RuntimeType runtimeType2 = (RuntimeType)runtimeType.GetGenericTypeDefinition();
				RuntimeType runtimeType3 = reflectedType;
				while (runtimeType3 != null)
				{
					RuntimeType runtimeType4 = runtimeType3;
					if (runtimeType4.IsGenericType && !runtimeType3.IsGenericTypeDefinition)
					{
						runtimeType4 = (RuntimeType)runtimeType4.GetGenericTypeDefinition();
					}
					if (runtimeType4 == runtimeType2)
					{
						break;
					}
					runtimeType3 = runtimeType3.GetBaseType();
				}
				if (runtimeType3 == null)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethodHandle, reflectedType, runtimeType));
				}
				runtimeType = runtimeType3;
				if (!RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle))
				{
					array = RuntimeMethodHandle.GetMethodInstantiationInternal(methodHandle);
				}
				methodHandle = RuntimeMethodHandle.GetMethodFromCanonical(methodHandle, runtimeType);
			}
			else if (!runtimeType.IsAssignableFrom(reflectedType))
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveMethodHandle, reflectedType.ToString(), runtimeType.ToString()));
			}
		}
		methodHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, runtimeType, array);
		MethodBase result = (RuntimeMethodHandle.IsConstructor(methodHandle) ? reflectedType.Cache.GetConstructor(runtimeType, methodHandle) : ((!RuntimeMethodHandle.HasMethodInstantiation(methodHandle) || RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle)) ? reflectedType.Cache.GetMethod(runtimeType, methodHandle) : reflectedType.Cache.GetGenericMethodInfo(methodHandle)));
		GC.KeepAlive(array);
		return result;
	}

	internal static FieldInfo GetFieldInfo(IRuntimeFieldInfo fieldHandle)
	{
		return GetFieldInfo(RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle), fieldHandle);
	}

	internal static FieldInfo GetFieldInfo(RuntimeType reflectedType, IRuntimeFieldInfo field)
	{
		RuntimeFieldHandleInternal value = field.Value;
		if (reflectedType == null)
		{
			reflectedType = RuntimeFieldHandle.GetApproxDeclaringType(value);
		}
		else
		{
			RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(value);
			if (reflectedType != approxDeclaringType && (!RuntimeFieldHandle.AcquiresContextFromThis(value) || !RuntimeTypeHandle.CompareCanonicalHandles(approxDeclaringType, reflectedType)))
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveFieldHandle, reflectedType, approxDeclaringType));
			}
		}
		FieldInfo field2 = reflectedType.Cache.GetField(value);
		GC.KeepAlive(field);
		return field2;
	}

	private static RuntimePropertyInfo GetPropertyInfo(RuntimeType reflectedType, int tkProperty)
	{
		RuntimePropertyInfo[] propertyList = reflectedType.Cache.GetPropertyList(MemberListType.All, null);
		foreach (RuntimePropertyInfo runtimePropertyInfo in propertyList)
		{
			if (runtimePropertyInfo.MetadataToken == tkProperty)
			{
				return runtimePropertyInfo;
			}
		}
		throw new UnreachableException();
	}

	internal static void ValidateGenericArguments(MemberInfo definition, RuntimeType[] genericArguments, Exception e)
	{
		RuntimeType[] typeContext = null;
		RuntimeType[] methodContext = null;
		RuntimeType[] genericArgumentsInternal;
		if (definition is Type)
		{
			RuntimeType runtimeType = (RuntimeType)definition;
			genericArgumentsInternal = runtimeType.GetGenericArgumentsInternal();
			typeContext = genericArguments;
		}
		else
		{
			RuntimeMethodInfo runtimeMethodInfo = (RuntimeMethodInfo)definition;
			genericArgumentsInternal = runtimeMethodInfo.GetGenericArgumentsInternal();
			methodContext = genericArguments;
			RuntimeType runtimeType2 = (RuntimeType)runtimeMethodInfo.DeclaringType;
			if (runtimeType2 != null)
			{
				typeContext = runtimeType2.TypeHandle.GetInstantiationInternal();
			}
		}
		for (int i = 0; i < genericArguments.Length; i++)
		{
			Type type = genericArguments[i];
			Type type2 = genericArgumentsInternal[i];
			if (!RuntimeTypeHandle.SatisfiesConstraints(type2.TypeHandle.GetTypeChecked(), typeContext, methodContext, type.TypeHandle.GetTypeChecked()))
			{
				throw new ArgumentException(SR.Format(SR.Argument_GenConstraintViolation, i.ToString(), type, definition, type2), e);
			}
		}
	}

	private static void SplitName(string fullname, out string name, out string ns)
	{
		name = null;
		ns = null;
		if (fullname == null)
		{
			return;
		}
		int num = fullname.LastIndexOf('.');
		if (num >= 0)
		{
			ns = fullname.Substring(0, num);
			int num2 = fullname.Length - ns.Length - 1;
			if (num2 != 0)
			{
				name = fullname.Substring(num + 1, num2);
			}
			else
			{
				name = "";
			}
		}
		else
		{
			name = fullname;
		}
	}

	internal static BindingFlags FilterPreCalculate(bool isPublic, bool isInherited, bool isStatic)
	{
		BindingFlags bindingFlags = (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);
		if (isInherited)
		{
			bindingFlags |= BindingFlags.DeclaredOnly;
			if (isStatic)
			{
				return bindingFlags | (BindingFlags.Static | BindingFlags.FlattenHierarchy);
			}
			return bindingFlags | BindingFlags.Instance;
		}
		if (isStatic)
		{
			return bindingFlags | BindingFlags.Static;
		}
		return bindingFlags | BindingFlags.Instance;
	}

	private static void FilterHelper(BindingFlags bindingFlags, ref string name, bool allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType)
	{
		prefixLookup = false;
		ignoreCase = false;
		if (name != null)
		{
			if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
			{
				name = name.ToLowerInvariant();
				ignoreCase = true;
				listType = MemberListType.CaseInsensitive;
			}
			else
			{
				listType = MemberListType.CaseSensitive;
			}
			if (allowPrefixLookup && name.EndsWith('*'))
			{
				string text = name;
				name = text.Substring(0, text.Length - 1);
				prefixLookup = true;
				listType = MemberListType.All;
			}
		}
		else
		{
			listType = MemberListType.All;
		}
	}

	private static void FilterHelper(BindingFlags bindingFlags, ref string name, out bool ignoreCase, out MemberListType listType)
	{
		FilterHelper(bindingFlags, ref name, allowPrefixLookup: false, out var _, out ignoreCase, out listType);
	}

	private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase)
	{
		if (ignoreCase)
		{
			if (!memberInfo.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		else if (!memberInfo.Name.StartsWith(name, StringComparison.Ordinal))
		{
			return false;
		}
		return true;
	}

	private static bool FilterApplyBase(MemberInfo memberInfo, BindingFlags bindingFlags, bool isPublic, bool isNonProtectedInternal, bool isStatic, string name, bool prefixLookup)
	{
		if (isPublic)
		{
			if ((bindingFlags & BindingFlags.Public) == 0)
			{
				return false;
			}
		}
		else if ((bindingFlags & BindingFlags.NonPublic) == 0)
		{
			return false;
		}
		bool flag = (object)memberInfo.DeclaringType != memberInfo.ReflectedType;
		if ((bindingFlags & BindingFlags.DeclaredOnly) != 0 && flag)
		{
			return false;
		}
		if (memberInfo.MemberType != MemberTypes.TypeInfo && memberInfo.MemberType != MemberTypes.NestedType)
		{
			if (isStatic)
			{
				if ((bindingFlags & BindingFlags.FlattenHierarchy) == 0 && flag)
				{
					return false;
				}
				if ((bindingFlags & BindingFlags.Static) == 0)
				{
					return false;
				}
			}
			else if ((bindingFlags & BindingFlags.Instance) == 0)
			{
				return false;
			}
		}
		if (prefixLookup && !FilterApplyPrefixLookup(memberInfo, name, (bindingFlags & BindingFlags.IgnoreCase) != 0))
		{
			return false;
		}
		if ((bindingFlags & BindingFlags.DeclaredOnly) == 0 && flag && isNonProtectedInternal && (bindingFlags & BindingFlags.NonPublic) != 0 && !isStatic && (bindingFlags & BindingFlags.Instance) != 0)
		{
			MethodInfo methodInfo = memberInfo as MethodInfo;
			if (methodInfo == null)
			{
				return false;
			}
			if (!methodInfo.IsVirtual && !methodInfo.IsAbstract)
			{
				return false;
			}
		}
		return true;
	}

	private static bool FilterApplyType(Type type, BindingFlags bindingFlags, string name, bool prefixLookup, string ns)
	{
		bool isPublic = type.IsNestedPublic || type.IsPublic;
		if (!FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic: false, name, prefixLookup))
		{
			return false;
		}
		if (ns != null && ns != type.Namespace)
		{
			return false;
		}
		return true;
	}

	private static bool FilterApplyMethodInfo(RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
	{
		return FilterApplyMethodBase(method, method.BindingFlags, bindingFlags, callConv, argumentTypes);
	}

	private static bool FilterApplyConstructorInfo(RuntimeConstructorInfo constructor, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
	{
		return FilterApplyMethodBase(constructor, constructor.BindingFlags, bindingFlags, callConv, argumentTypes);
	}

	private static bool FilterApplyMethodBase(MethodBase methodBase, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
	{
		bindingFlags ^= BindingFlags.DeclaredOnly;
		if ((bindingFlags & methodFlags) != methodFlags)
		{
			return false;
		}
		if ((callConv & CallingConventions.Any) == 0)
		{
			if ((callConv & CallingConventions.VarArgs) != 0 && (methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
			{
				return false;
			}
			if ((callConv & CallingConventions.Standard) != 0 && (methodBase.CallingConvention & CallingConventions.Standard) == 0)
			{
				return false;
			}
		}
		if (argumentTypes != null)
		{
			ParameterInfo[] parametersNoCopy = methodBase.GetParametersNoCopy();
			if (argumentTypes.Length != parametersNoCopy.Length)
			{
				if ((bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.SetProperty)) == 0)
				{
					return false;
				}
				bool flag = false;
				if (argumentTypes.Length > parametersNoCopy.Length)
				{
					if ((methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
					{
						flag = true;
					}
				}
				else if ((bindingFlags & BindingFlags.OptionalParamBinding) == 0)
				{
					flag = true;
				}
				else if (!parametersNoCopy[argumentTypes.Length].IsOptional)
				{
					flag = true;
				}
				if (flag)
				{
					if (parametersNoCopy.Length == 0)
					{
						return false;
					}
					if (argumentTypes.Length < parametersNoCopy.Length - 1)
					{
						return false;
					}
					ParameterInfo parameterInfo = parametersNoCopy[^1];
					if (!parameterInfo.ParameterType.IsArray)
					{
						return false;
					}
					if (!parameterInfo.IsDefined(typeof(ParamArrayAttribute), inherit: false))
					{
						return false;
					}
				}
			}
			else if ((bindingFlags & BindingFlags.ExactBinding) != 0 && (bindingFlags & BindingFlags.InvokeMethod) == 0)
			{
				for (int i = 0; i < parametersNoCopy.Length; i++)
				{
					Type type = argumentTypes[i];
					if ((object)type != null && !type.MatchesParameterTypeExactly(parametersNoCopy[i]))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	internal RuntimeType()
	{
		throw new NotSupportedException();
	}

	internal unsafe TypeHandle GetNativeTypeHandle()
	{
		return new TypeHandle((void*)m_handle);
	}

	internal nint GetUnderlyingNativeHandle()
	{
		return m_handle;
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeType runtimeType)
		{
			return runtimeType.m_handle == m_handle;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private RuntimeTypeCache InitializeCache()
	{
		if (m_cache == IntPtr.Zero)
		{
			RuntimeTypeHandle runtimeTypeHandle = new RuntimeTypeHandle(this);
			nint gCHandle = runtimeTypeHandle.GetGCHandle(GCHandleType.WeakTrackResurrection);
			nint num = Interlocked.CompareExchange(ref m_cache, gCHandle, IntPtr.Zero);
			if (num != IntPtr.Zero)
			{
				runtimeTypeHandle.FreeGCHandle(gCHandle);
			}
		}
		RuntimeTypeCache runtimeTypeCache = (RuntimeTypeCache)GCHandle.InternalGet(m_cache);
		if (runtimeTypeCache == null)
		{
			runtimeTypeCache = new RuntimeTypeCache(this);
			RuntimeTypeCache runtimeTypeCache2 = (RuntimeTypeCache)GCHandle.InternalCompareExchange(m_cache, runtimeTypeCache, null);
			if (runtimeTypeCache2 != null)
			{
				runtimeTypeCache = runtimeTypeCache2;
			}
		}
		return runtimeTypeCache;
	}

	internal void ClearCache()
	{
		if (Volatile.Read(ref m_cache) != IntPtr.Zero)
		{
			GCHandle.InternalSet(m_cache, null);
		}
	}

	private string GetDefaultMemberName()
	{
		return Cache.GetDefaultMemberName();
	}

	private ListBuilder<MethodInfo> GetMethodCandidates(string name, int genericParameterCount, BindingFlags bindingAttr, CallingConventions callConv, Type[] types, bool allowPrefixLookup)
	{
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var ignoreCase, out var listType);
		RuntimeMethodInfo[] methodList = Cache.GetMethodList(listType, name);
		ListBuilder<MethodInfo> result = new ListBuilder<MethodInfo>(methodList.Length);
		foreach (RuntimeMethodInfo runtimeMethodInfo in methodList)
		{
			if ((genericParameterCount == -1 || genericParameterCount == runtimeMethodInfo.GenericParameterCount) && FilterApplyMethodInfo(runtimeMethodInfo, bindingAttr, callConv, types) && (!prefixLookup || FilterApplyPrefixLookup(runtimeMethodInfo, name, ignoreCase)))
			{
				result.Add(runtimeMethodInfo);
			}
		}
		return result;
	}

	private ListBuilder<ConstructorInfo> GetConstructorCandidates(string name, BindingFlags bindingAttr, CallingConventions callConv, Type[] types, bool allowPrefixLookup)
	{
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var ignoreCase, out var listType);
		RuntimeConstructorInfo[] constructorList = Cache.GetConstructorList(listType, name);
		ListBuilder<ConstructorInfo> result = new ListBuilder<ConstructorInfo>(constructorList.Length);
		foreach (RuntimeConstructorInfo runtimeConstructorInfo in constructorList)
		{
			if (FilterApplyConstructorInfo(runtimeConstructorInfo, bindingAttr, callConv, types) && (!prefixLookup || FilterApplyPrefixLookup(runtimeConstructorInfo, name, ignoreCase)))
			{
				result.Add(runtimeConstructorInfo);
			}
		}
		return result;
	}

	private ListBuilder<PropertyInfo> GetPropertyCandidates(string name, BindingFlags bindingAttr, Type[] types, bool allowPrefixLookup)
	{
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var ignoreCase, out var listType);
		RuntimePropertyInfo[] propertyList = Cache.GetPropertyList(listType, name);
		bindingAttr ^= BindingFlags.DeclaredOnly;
		ListBuilder<PropertyInfo> result = new ListBuilder<PropertyInfo>(propertyList.Length);
		foreach (RuntimePropertyInfo runtimePropertyInfo in propertyList)
		{
			if ((bindingAttr & runtimePropertyInfo.BindingFlags) == runtimePropertyInfo.BindingFlags && (!prefixLookup || FilterApplyPrefixLookup(runtimePropertyInfo, name, ignoreCase)) && (types == null || runtimePropertyInfo.GetIndexParameters().Length == types.Length))
			{
				result.Add(runtimePropertyInfo);
			}
		}
		return result;
	}

	private ListBuilder<EventInfo> GetEventCandidates(string name, BindingFlags bindingAttr, bool allowPrefixLookup)
	{
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var ignoreCase, out var listType);
		RuntimeEventInfo[] eventList = Cache.GetEventList(listType, name);
		bindingAttr ^= BindingFlags.DeclaredOnly;
		ListBuilder<EventInfo> result = new ListBuilder<EventInfo>(eventList.Length);
		foreach (RuntimeEventInfo runtimeEventInfo in eventList)
		{
			if ((bindingAttr & runtimeEventInfo.BindingFlags) == runtimeEventInfo.BindingFlags && (!prefixLookup || FilterApplyPrefixLookup(runtimeEventInfo, name, ignoreCase)))
			{
				result.Add(runtimeEventInfo);
			}
		}
		return result;
	}

	private ListBuilder<FieldInfo> GetFieldCandidates(string name, BindingFlags bindingAttr, bool allowPrefixLookup)
	{
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var ignoreCase, out var listType);
		RuntimeFieldInfo[] fieldList = Cache.GetFieldList(listType, name);
		bindingAttr ^= BindingFlags.DeclaredOnly;
		ListBuilder<FieldInfo> result = new ListBuilder<FieldInfo>(fieldList.Length);
		foreach (RuntimeFieldInfo runtimeFieldInfo in fieldList)
		{
			if ((bindingAttr & runtimeFieldInfo.BindingFlags) == runtimeFieldInfo.BindingFlags && (!prefixLookup || FilterApplyPrefixLookup(runtimeFieldInfo, name, ignoreCase)))
			{
				result.Add(runtimeFieldInfo);
			}
		}
		return result;
	}

	private ListBuilder<Type> GetNestedTypeCandidates(string fullname, BindingFlags bindingAttr, bool allowPrefixLookup)
	{
		bindingAttr &= ~BindingFlags.Static;
		SplitName(fullname, out var name, out var ns);
		FilterHelper(bindingAttr, ref name, allowPrefixLookup, out var prefixLookup, out var _, out var listType);
		RuntimeType[] nestedTypeList = Cache.GetNestedTypeList(listType, name);
		ListBuilder<Type> result = new ListBuilder<Type>(nestedTypeList.Length);
		foreach (RuntimeType runtimeType in nestedTypeList)
		{
			if (FilterApplyType(runtimeType, bindingAttr, name, prefixLookup, ns))
			{
				result.Add(runtimeType);
			}
		}
		return result;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		return GetMethodCandidates(null, -1, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		return GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		return GetPropertyCandidates(null, bindingAttr, null, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		return GetEventCandidates(null, bindingAttr, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		return GetFieldCandidates(null, bindingAttr, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		RuntimeType[] interfaceList = Cache.GetInterfaceList(MemberListType.All, null);
		Type[] array = interfaceList;
		return new ReadOnlySpan<Type>(array).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		return GetNestedTypeCandidates(null, bindingAttr, allowPrefixLookup: false).ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		ListBuilder<MethodInfo> methodCandidates = GetMethodCandidates(null, -1, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: false);
		ListBuilder<ConstructorInfo> constructorCandidates = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: false);
		ListBuilder<PropertyInfo> propertyCandidates = GetPropertyCandidates(null, bindingAttr, null, allowPrefixLookup: false);
		ListBuilder<EventInfo> eventCandidates = GetEventCandidates(null, bindingAttr, allowPrefixLookup: false);
		ListBuilder<FieldInfo> fieldCandidates = GetFieldCandidates(null, bindingAttr, allowPrefixLookup: false);
		ListBuilder<Type> nestedTypeCandidates = GetNestedTypeCandidates(null, bindingAttr, allowPrefixLookup: false);
		MemberInfo[] array = new MemberInfo[methodCandidates.Count + constructorCandidates.Count + propertyCandidates.Count + eventCandidates.Count + fieldCandidates.Count + nestedTypeCandidates.Count];
		int num = 0;
		object[] array2 = array;
		methodCandidates.CopyTo(array2, num);
		num += methodCandidates.Count;
		array2 = array;
		constructorCandidates.CopyTo(array2, num);
		num += constructorCandidates.Count;
		array2 = array;
		propertyCandidates.CopyTo(array2, num);
		num += propertyCandidates.Count;
		array2 = array;
		eventCandidates.CopyTo(array2, num);
		num += eventCandidates.Count;
		array2 = array;
		fieldCandidates.CopyTo(array2, num);
		num += fieldCandidates.Count;
		array2 = array;
		nestedTypeCandidates.CopyTo(array2, num);
		num += nestedTypeCandidates.Count;
		return array;
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		if (IsGenericParameter)
		{
			throw new InvalidOperationException(SR.Arg_GenericParameter);
		}
		ArgumentNullException.ThrowIfNull(interfaceType, "interfaceType");
		RuntimeType runtimeType = interfaceType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "interfaceType");
		}
		RuntimeTypeHandle typeHandle = runtimeType.TypeHandle;
		TypeHandle.VerifyInterfaceIsImplemented(typeHandle);
		if (IsSZArray && interfaceType.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_ArrayGetInterfaceMap);
		}
		int numVirtualsAndStaticVirtuals = RuntimeTypeHandle.GetNumVirtualsAndStaticVirtuals(runtimeType);
		Unsafe.SkipInit(out InterfaceMapping result);
		result.InterfaceType = interfaceType;
		result.TargetType = this;
		result.InterfaceMethods = new MethodInfo[numVirtualsAndStaticVirtuals];
		result.TargetMethods = new MethodInfo[numVirtualsAndStaticVirtuals];
		for (int i = 0; i < numVirtualsAndStaticVirtuals; i++)
		{
			RuntimeMethodHandleInternal methodAt = RuntimeTypeHandle.GetMethodAt(runtimeType, i);
			MethodBase methodBase = GetMethodBase(runtimeType, methodAt);
			result.InterfaceMethods[i] = (MethodInfo)methodBase;
			RuntimeMethodHandleInternal interfaceMethodImplementation = TypeHandle.GetInterfaceMethodImplementation(typeHandle, methodAt);
			if (!interfaceMethodImplementation.IsNullHandle())
			{
				RuntimeType runtimeType2 = RuntimeMethodHandle.GetDeclaringType(interfaceMethodImplementation);
				if (!runtimeType2.IsInterface)
				{
					runtimeType2 = this;
				}
				MethodBase methodBase2 = GetMethodBase(runtimeType2, interfaceMethodImplementation);
				result.TargetMethods[i] = (MethodInfo)methodBase2;
			}
		}
		return result;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
	{
		return GetMethodImplCommon(name, -1, bindingAttr, binder, callConv, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
	{
		return GetMethodImplCommon(name, genericParameterCount, bindingAttr, binder, callConv, types, modifiers);
	}

	private MethodInfo GetMethodImplCommon(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
	{
		ListBuilder<MethodInfo> methodCandidates = GetMethodCandidates(name, genericParameterCount, bindingAttr, callConv, types, allowPrefixLookup: false);
		if (methodCandidates.Count == 0)
		{
			return null;
		}
		MethodBase[] match;
		if (types == null || types.Length == 0)
		{
			MethodInfo methodInfo = methodCandidates[0];
			if (methodCandidates.Count == 1)
			{
				return methodInfo;
			}
			if (types == null)
			{
				for (int i = 1; i < methodCandidates.Count; i++)
				{
					MethodInfo m = methodCandidates[i];
					if (!System.DefaultBinder.CompareMethodSig(m, methodInfo))
					{
						throw ThrowHelper.GetAmbiguousMatchException(methodInfo);
					}
				}
				match = methodCandidates.ToArray();
				return System.DefaultBinder.FindMostDerivedNewSlotMeth(match, methodCandidates.Count) as MethodInfo;
			}
		}
		if (binder == null)
		{
			binder = Type.DefaultBinder;
		}
		Binder binder2 = binder;
		match = methodCandidates.ToArray();
		return binder2.SelectMethod(bindingAttr, match, types, modifiers) as MethodInfo;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		ListBuilder<ConstructorInfo> constructorCandidates = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, types, allowPrefixLookup: false);
		if (constructorCandidates.Count == 0)
		{
			return null;
		}
		if (types.Length == 0 && constructorCandidates.Count == 1)
		{
			ConstructorInfo constructorInfo = constructorCandidates[0];
			ParameterInfo[] parametersNoCopy = constructorInfo.GetParametersNoCopy();
			if (parametersNoCopy == null || parametersNoCopy.Length == 0)
			{
				return constructorInfo;
			}
		}
		MethodBase[] match;
		if ((bindingAttr & BindingFlags.ExactBinding) != 0)
		{
			match = constructorCandidates.ToArray();
			return System.DefaultBinder.ExactBinding(match, types) as ConstructorInfo;
		}
		if (binder == null)
		{
			binder = Type.DefaultBinder;
		}
		Binder binder2 = binder;
		match = constructorCandidates.ToArray();
		return binder2.SelectMethod(bindingAttr, match, types, modifiers) as ConstructorInfo;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		ListBuilder<PropertyInfo> propertyCandidates = GetPropertyCandidates(name, bindingAttr, types, allowPrefixLookup: false);
		if (propertyCandidates.Count == 0)
		{
			return null;
		}
		if (types == null || types.Length == 0)
		{
			PropertyInfo propertyInfo = propertyCandidates[0];
			if (propertyCandidates.Count == 1)
			{
				if ((object)returnType != null && !returnType.IsEquivalentTo(propertyInfo.PropertyType))
				{
					return null;
				}
				return propertyInfo;
			}
			if ((object)returnType == null)
			{
				throw ThrowHelper.GetAmbiguousMatchException(propertyInfo);
			}
		}
		if ((bindingAttr & BindingFlags.ExactBinding) != 0)
		{
			return System.DefaultBinder.ExactPropertyBinding(propertyCandidates.ToArray(), returnType, types);
		}
		if (binder == null)
		{
			binder = Type.DefaultBinder;
		}
		return binder.SelectProperty(bindingAttr, propertyCandidates.ToArray(), returnType, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		FilterHelper(bindingAttr, ref name, out var _, out var listType);
		RuntimeEventInfo[] eventList = Cache.GetEventList(listType, name);
		EventInfo eventInfo = null;
		bindingAttr ^= BindingFlags.DeclaredOnly;
		foreach (RuntimeEventInfo runtimeEventInfo in eventList)
		{
			if ((bindingAttr & runtimeEventInfo.BindingFlags) == runtimeEventInfo.BindingFlags)
			{
				if (eventInfo != null)
				{
					throw ThrowHelper.GetAmbiguousMatchException(eventInfo);
				}
				eventInfo = runtimeEventInfo;
			}
		}
		return eventInfo;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		FilterHelper(bindingAttr, ref name, out var _, out var listType);
		RuntimeFieldInfo[] fieldList = Cache.GetFieldList(listType, name);
		FieldInfo fieldInfo = null;
		bindingAttr ^= BindingFlags.DeclaredOnly;
		bool flag = false;
		foreach (RuntimeFieldInfo runtimeFieldInfo in fieldList)
		{
			if ((bindingAttr & runtimeFieldInfo.BindingFlags) != runtimeFieldInfo.BindingFlags)
			{
				continue;
			}
			if (fieldInfo != null)
			{
				if ((object)runtimeFieldInfo.DeclaringType == fieldInfo.DeclaringType)
				{
					throw ThrowHelper.GetAmbiguousMatchException(fieldInfo);
				}
				if (fieldInfo.DeclaringType.IsInterface && runtimeFieldInfo.DeclaringType.IsInterface)
				{
					flag = true;
				}
			}
			if (fieldInfo == null || runtimeFieldInfo.DeclaringType.IsSubclassOf(fieldInfo.DeclaringType) || fieldInfo.DeclaringType.IsInterface)
			{
				fieldInfo = runtimeFieldInfo;
			}
		}
		if (flag && fieldInfo.DeclaringType.IsInterface)
		{
			throw ThrowHelper.GetAmbiguousMatchException(fieldInfo);
		}
		return fieldInfo;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern", Justification = "Trimming makes sure that interfaces are fully preserved, so the Interfaces annotation is transitive.The cache doesn't carry the necessary annotation since it returns an array type,so the analysis complains that the returned value doesn't have the necessary annotation.")]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string fullname, bool ignoreCase)
	{
		ArgumentNullException.ThrowIfNull(fullname, "fullname");
		BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
		bindingFlags &= ~BindingFlags.Static;
		if (ignoreCase)
		{
			bindingFlags |= BindingFlags.IgnoreCase;
		}
		SplitName(fullname, out var name, out var ns);
		FilterHelper(bindingFlags, ref name, out var _, out var listType);
		RuntimeType[] interfaceList = Cache.GetInterfaceList(listType, name);
		RuntimeType runtimeType = null;
		foreach (RuntimeType runtimeType2 in interfaceList)
		{
			if (FilterApplyType(runtimeType2, bindingFlags, name, prefixLookup: false, ns))
			{
				if (runtimeType != null)
				{
					throw ThrowHelper.GetAmbiguousMatchException(runtimeType);
				}
				runtimeType = runtimeType2;
			}
		}
		return runtimeType;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string fullname, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(fullname, "fullname");
		bindingAttr &= ~BindingFlags.Static;
		SplitName(fullname, out var name, out var ns);
		FilterHelper(bindingAttr, ref name, out var _, out var listType);
		RuntimeType[] nestedTypeList = Cache.GetNestedTypeList(listType, name);
		RuntimeType runtimeType = null;
		foreach (RuntimeType runtimeType2 in nestedTypeList)
		{
			if (FilterApplyType(runtimeType2, bindingAttr, name, prefixLookup: false, ns))
			{
				if (runtimeType != null)
				{
					throw ThrowHelper.GetAmbiguousMatchException(runtimeType);
				}
				runtimeType = runtimeType2;
			}
		}
		return runtimeType;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		ListBuilder<MethodInfo> listBuilder = default(ListBuilder<MethodInfo>);
		ListBuilder<ConstructorInfo> listBuilder2 = default(ListBuilder<ConstructorInfo>);
		ListBuilder<PropertyInfo> listBuilder3 = default(ListBuilder<PropertyInfo>);
		ListBuilder<EventInfo> listBuilder4 = default(ListBuilder<EventInfo>);
		ListBuilder<FieldInfo> listBuilder5 = default(ListBuilder<FieldInfo>);
		ListBuilder<Type> listBuilder6 = default(ListBuilder<Type>);
		int num = 0;
		if ((type & MemberTypes.Method) != 0)
		{
			listBuilder = GetMethodCandidates(name, -1, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: true);
			if (type == MemberTypes.Method)
			{
				return listBuilder.ToArray();
			}
			num += listBuilder.Count;
		}
		if ((type & MemberTypes.Constructor) != 0)
		{
			listBuilder2 = GetConstructorCandidates(name, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: true);
			if (type == MemberTypes.Constructor)
			{
				return listBuilder2.ToArray();
			}
			num += listBuilder2.Count;
		}
		if ((type & MemberTypes.Property) != 0)
		{
			listBuilder3 = GetPropertyCandidates(name, bindingAttr, null, allowPrefixLookup: true);
			if (type == MemberTypes.Property)
			{
				return listBuilder3.ToArray();
			}
			num += listBuilder3.Count;
		}
		if ((type & MemberTypes.Event) != 0)
		{
			listBuilder4 = GetEventCandidates(name, bindingAttr, allowPrefixLookup: true);
			if (type == MemberTypes.Event)
			{
				return listBuilder4.ToArray();
			}
			num += listBuilder4.Count;
		}
		if ((type & MemberTypes.Field) != 0)
		{
			listBuilder5 = GetFieldCandidates(name, bindingAttr, allowPrefixLookup: true);
			if (type == MemberTypes.Field)
			{
				return listBuilder5.ToArray();
			}
			num += listBuilder5.Count;
		}
		if ((type & (MemberTypes.TypeInfo | MemberTypes.NestedType)) != 0)
		{
			listBuilder6 = GetNestedTypeCandidates(name, bindingAttr, allowPrefixLookup: true);
			if (type == MemberTypes.NestedType || type == MemberTypes.TypeInfo)
			{
				return listBuilder6.ToArray();
			}
			num += listBuilder6.Count;
		}
		MemberInfo[] array;
		if (type != (MemberTypes.Constructor | MemberTypes.Method))
		{
			array = new MemberInfo[num];
		}
		else
		{
			MemberInfo[] array2 = new MethodBase[num];
			array = array2;
		}
		MemberInfo[] array3 = array;
		int num2 = 0;
		object[] array4 = array3;
		listBuilder.CopyTo(array4, num2);
		num2 += listBuilder.Count;
		array4 = array3;
		listBuilder2.CopyTo(array4, num2);
		num2 += listBuilder2.Count;
		array4 = array3;
		listBuilder3.CopyTo(array4, num2);
		num2 += listBuilder3.Count;
		array4 = array3;
		listBuilder4.CopyTo(array4, num2);
		num2 += listBuilder4.Count;
		array4 = array3;
		listBuilder5.CopyTo(array4, num2);
		num2 += listBuilder5.Count;
		array4 = array3;
		listBuilder6.CopyTo(array4, num2);
		num2 += listBuilder6.Count;
		return array3;
	}

	public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
	{
		ArgumentNullException.ThrowIfNull(member, "member");
		RuntimeType runtimeType = this;
		while (runtimeType != null)
		{
			MemberInfo memberInfo = member.MemberType switch
			{
				MemberTypes.Method => GetMethodWithSameMetadataDefinitionAs(runtimeType, member), 
				MemberTypes.Constructor => GetConstructorWithSameMetadataDefinitionAs(runtimeType, member), 
				MemberTypes.Property => GetPropertyWithSameMetadataDefinitionAs(runtimeType, member), 
				MemberTypes.Field => GetFieldWithSameMetadataDefinitionAs(runtimeType, member), 
				MemberTypes.Event => GetEventWithSameMetadataDefinitionAs(runtimeType, member), 
				MemberTypes.NestedType => GetNestedTypeWithSameMetadataDefinitionAs(runtimeType, member), 
				_ => null, 
			};
			if (memberInfo != null)
			{
				return memberInfo;
			}
			runtimeType = runtimeType.GetBaseType();
		}
		throw Type.CreateGetMemberWithSameMetadataDefinitionAsNotFoundException(member);
	}

	private static RuntimeMethodInfo GetMethodWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo method)
	{
		RuntimeMethodInfo[] methodList = runtimeType.Cache.GetMethodList(MemberListType.CaseSensitive, method.Name);
		foreach (RuntimeMethodInfo runtimeMethodInfo in methodList)
		{
			if (runtimeMethodInfo.HasSameMetadataDefinitionAs(method))
			{
				return runtimeMethodInfo;
			}
		}
		return null;
	}

	private static RuntimeConstructorInfo GetConstructorWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo constructor)
	{
		RuntimeConstructorInfo[] constructorList = runtimeType.Cache.GetConstructorList(MemberListType.CaseSensitive, constructor.Name);
		foreach (RuntimeConstructorInfo runtimeConstructorInfo in constructorList)
		{
			if (runtimeConstructorInfo.HasSameMetadataDefinitionAs(constructor))
			{
				return runtimeConstructorInfo;
			}
		}
		return null;
	}

	private static RuntimePropertyInfo GetPropertyWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo property)
	{
		RuntimePropertyInfo[] propertyList = runtimeType.Cache.GetPropertyList(MemberListType.CaseSensitive, property.Name);
		foreach (RuntimePropertyInfo runtimePropertyInfo in propertyList)
		{
			if (runtimePropertyInfo.HasSameMetadataDefinitionAs(property))
			{
				return runtimePropertyInfo;
			}
		}
		return null;
	}

	private static RuntimeFieldInfo GetFieldWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo field)
	{
		RuntimeFieldInfo[] fieldList = runtimeType.Cache.GetFieldList(MemberListType.CaseSensitive, field.Name);
		foreach (RuntimeFieldInfo runtimeFieldInfo in fieldList)
		{
			if (runtimeFieldInfo.HasSameMetadataDefinitionAs(field))
			{
				return runtimeFieldInfo;
			}
		}
		return null;
	}

	private static RuntimeEventInfo GetEventWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo eventInfo)
	{
		RuntimeEventInfo[] eventList = runtimeType.Cache.GetEventList(MemberListType.CaseSensitive, eventInfo.Name);
		foreach (RuntimeEventInfo runtimeEventInfo in eventList)
		{
			if (runtimeEventInfo.HasSameMetadataDefinitionAs(eventInfo))
			{
				return runtimeEventInfo;
			}
		}
		return null;
	}

	private static RuntimeType GetNestedTypeWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo nestedType)
	{
		RuntimeType[] nestedTypeList = runtimeType.Cache.GetNestedTypeList(MemberListType.CaseSensitive, nestedType.Name);
		foreach (RuntimeType runtimeType2 in nestedTypeList)
		{
			if (runtimeType2.HasSameMetadataDefinitionAs(nestedType))
			{
				return runtimeType2;
			}
		}
		return null;
	}

	public override bool IsSubclassOf(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		RuntimeType runtimeType = type as RuntimeType;
		if (runtimeType == null)
		{
			return false;
		}
		RuntimeType baseType = GetBaseType();
		while (baseType != null)
		{
			if (baseType == runtimeType)
			{
				return true;
			}
			baseType = baseType.GetBaseType();
		}
		if (runtimeType == ObjectType && runtimeType != this)
		{
			return true;
		}
		return false;
	}

	public override bool IsEquivalentTo([NotNullWhen(true)] Type other)
	{
		if (!(other is RuntimeType runtimeType))
		{
			return false;
		}
		if (runtimeType == this)
		{
			return true;
		}
		return RuntimeTypeHandle.IsEquivalentTo(this, runtimeType);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void GetGUID(ref Guid result);

	protected unsafe override bool IsValueTypeImpl()
	{
		TypeHandle nativeTypeHandle = GetNativeTypeHandle();
		if (nativeTypeHandle.IsTypeDesc)
		{
			return IsSubclassOf(typeof(ValueType));
		}
		bool isValueType = nativeTypeHandle.AsMethodTable()->IsValueType;
		GC.KeepAlive(this);
		return isValueType;
	}

	internal unsafe bool IsDelegate()
	{
		TypeHandle nativeTypeHandle = GetNativeTypeHandle();
		bool result = !nativeTypeHandle.IsTypeDesc && nativeTypeHandle.AsMethodTable()->ParentMethodTable == System.Runtime.CompilerServices.TypeHandle.TypeHandleOf<MulticastDelegate>().AsMethodTable();
		GC.KeepAlive(this);
		return result;
	}

	public override Type GetGenericTypeDefinition()
	{
		if (!IsGenericType)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
		}
		return Cache.GetGenericTypeDefinition();
	}

	internal object[] GetEmptyArray()
	{
		return Cache.GetEmptyArray();
	}

	internal RuntimeType[] GetGenericArgumentsInternal()
	{
		return GetRootElementType().TypeHandle.GetInstantiationInternal();
	}

	public override Type[] GetGenericArguments()
	{
		Type[] instantiationPublic = GetRootElementType().TypeHandle.GetInstantiationPublic();
		return instantiationPublic ?? Type.EmptyTypes;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override Type MakeGenericType(params Type[] instantiation)
	{
		ArgumentNullException.ThrowIfNull(instantiation, "instantiation");
		if (!IsGenericTypeDefinition)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));
		}
		RuntimeType[] genericArgumentsInternal = GetGenericArgumentsInternal();
		if (genericArgumentsInternal.Length != instantiation.Length)
		{
			throw new ArgumentException(SR.Argument_GenericArgsCount, "instantiation");
		}
		if (instantiation.Length == 1 && instantiation[0] is RuntimeType runtimeType)
		{
			ThrowIfTypeNeverValidGenericArgument(runtimeType);
			try
			{
				return new RuntimeTypeHandle(this).Instantiate(runtimeType);
			}
			catch (TypeLoadException e)
			{
				ValidateGenericArguments(this, new RuntimeType[1] { runtimeType }, e);
				throw;
			}
		}
		RuntimeType[] array = new RuntimeType[instantiation.Length];
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < instantiation.Length; i++)
		{
			Type type = instantiation[i];
			if (type == null)
			{
				throw new ArgumentNullException();
			}
			RuntimeType runtimeType2 = type as RuntimeType;
			if (runtimeType2 == null)
			{
				flag2 = true;
				if (type.IsSignatureType)
				{
					flag = true;
				}
			}
			array[i] = runtimeType2;
		}
		if (flag2)
		{
			if (flag)
			{
				return new SignatureConstructedGenericType(this, instantiation);
			}
			return TypeBuilderInstantiation.MakeGenericType(this, (Type[])instantiation.Clone());
		}
		SanityCheckGenericArguments(array, genericArgumentsInternal);
		try
		{
			RuntimeTypeHandle runtimeTypeHandle = new RuntimeTypeHandle(this);
			Type[] inst = array;
			return runtimeTypeHandle.Instantiate(inst);
		}
		catch (TypeLoadException e2)
		{
			ValidateGenericArguments(this, array, e2);
			throw;
		}
	}

	public override Type[] GetGenericParameterConstraints()
	{
		if (!IsGenericParameter)
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
		Type[] constraints = new RuntimeTypeHandle(this).GetConstraints();
		return constraints ?? Type.EmptyTypes;
	}

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeType>(other);
	}

	public override Type MakePointerType()
	{
		return new RuntimeTypeHandle(this).MakePointer();
	}

	public override Type MakeByRefType()
	{
		return new RuntimeTypeHandle(this).MakeByRef();
	}

	public override Type MakeArrayType()
	{
		return new RuntimeTypeHandle(this).MakeSZArray();
	}

	public override Type MakeArrayType(int rank)
	{
		if (rank <= 0)
		{
			throw new IndexOutOfRangeException();
		}
		return new RuntimeTypeHandle(this).MakeArray(rank);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool CanValueSpecialCast(RuntimeType valueType, RuntimeType targetType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object AllocateValueType(RuntimeType type, object value);

	private CheckValueStatus TryChangeTypeSpecial(ref object value)
	{
		Pointer pointer = value as Pointer;
		RuntimeType runtimeType = ((pointer != null) ? pointer.GetPointerType() : ((RuntimeType)value.GetType()));
		if (!CanValueSpecialCast(runtimeType, this))
		{
			return CheckValueStatus.ArgumentException;
		}
		if (pointer != null)
		{
			value = pointer.GetPointerValue();
		}
		else
		{
			CorElementType underlyingType = GetUnderlyingType(runtimeType);
			CorElementType underlyingType2 = GetUnderlyingType(this);
			if (underlyingType2 != underlyingType)
			{
				value = InvokeUtils.ConvertOrWiden(runtimeType, value, this, underlyingType2);
			}
		}
		return CheckValueStatus.Success;
	}

	public override Type[] GetFunctionPointerCallingConventions()
	{
		if (!IsFunctionPointer)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);
		}
		return Type.EmptyTypes;
	}

	public override Type[] GetFunctionPointerParameterTypes()
	{
		if (!IsFunctionPointer)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);
		}
		Type[] functionPointerReturnAndParameterTypes = Cache.FunctionPointerReturnAndParameterTypes;
		if (functionPointerReturnAndParameterTypes.Length == 1)
		{
			return Type.EmptyTypes;
		}
		return functionPointerReturnAndParameterTypes.AsSpan(1).ToArray();
	}

	public override Type GetFunctionPointerReturnType()
	{
		if (!IsFunctionPointer)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);
		}
		return Cache.FunctionPointerReturnAndParameterTypes[0];
	}

	public override string ToString()
	{
		return GetCachedName(TypeNameKind.ToString);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string GetCachedName(TypeNameKind kind)
	{
		return Cache.GetName(kind);
	}

	private void CreateInstanceCheckThis()
	{
		if (ContainsGenericParameters)
		{
			throw new ArgumentException(SR.Format(SR.Acc_CreateGenericEx, this));
		}
		Type rootElementType = GetRootElementType();
		if ((object)rootElementType == typeof(ArgIterator))
		{
			throw new NotSupportedException(SR.Acc_CreateArgIterator);
		}
		if ((object)rootElementType == typeof(void))
		{
			throw new NotSupportedException(SR.Acc_CreateVoid);
		}
	}

	internal object CreateInstanceImpl(BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture)
	{
		CreateInstanceCheckThis();
		if (args == null)
		{
			args = Array.Empty<object>();
		}
		if (binder == null)
		{
			binder = Type.DefaultBinder;
		}
		bool publicOnly = (bindingAttr & BindingFlags.NonPublic) == 0;
		bool wrapExceptions2 = (bindingAttr & BindingFlags.DoNotWrapExceptions) == 0;
		object result;
		if (args.Length == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0 && (IsGenericCOMObjectImpl() || base.IsValueType))
		{
			result = CreateInstanceDefaultCtor(publicOnly, wrapExceptions2);
		}
		else
		{
			ListBuilder<ConstructorInfo> constructorCandidates = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, allowPrefixLookup: false);
			MethodBase[] array = new MethodBase[constructorCandidates.Count];
			int num = 0;
			Type[] array2 = ((args.Length != 0) ? new Type[args.Length] : Type.EmptyTypes);
			for (int i = 0; i < args.Length; i++)
			{
				object obj = args[i];
				if (obj != null)
				{
					array2[i] = obj.GetType();
				}
			}
			for (int j = 0; j < constructorCandidates.Count; j++)
			{
				if (FilterApplyConstructorInfo((RuntimeConstructorInfo)constructorCandidates[j], bindingAttr, CallingConventions.Any, array2))
				{
					array[num++] = constructorCandidates[j];
				}
			}
			if (num == 0)
			{
				throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, FullName));
			}
			if (num != array.Length)
			{
				Array.Resize(ref array, num);
			}
			object state = null;
			MethodBase methodBase;
			try
			{
				methodBase = binder.BindToMethod(bindingAttr, array, ref args, null, culture, null, out state);
			}
			catch (MissingMethodException)
			{
				methodBase = null;
			}
			if ((object)methodBase == null)
			{
				throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, FullName));
			}
			if (methodBase.GetParametersNoCopy().Length == 0)
			{
				if (args.Length != 0)
				{
					throw new NotSupportedException(SR.NotSupported_CallToVarArg);
				}
				result = CreateInstanceLocal(wrapExceptions2);
			}
			else
			{
				result = ((ConstructorInfo)methodBase).Invoke(bindingAttr, binder, args, culture);
				if (state != null)
				{
					binder.ReorderArgumentArray(ref args, state);
				}
			}
		}
		return result;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
		object CreateInstanceLocal(bool wrapExceptions)
		{
			return Activator.CreateInstance(this, nonPublic: true, wrapExceptions);
		}
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	internal object CreateInstanceDefaultCtor(bool publicOnly, bool wrapExceptions)
	{
		ActivatorCache activatorCache = GenericCache as ActivatorCache;
		if (activatorCache == null)
		{
			activatorCache = (ActivatorCache)(GenericCache = new ActivatorCache(this));
		}
		if (!activatorCache.CtorIsPublic && publicOnly)
		{
			throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
		}
		object obj = activatorCache.CreateUninitializedObject(this);
		try
		{
			activatorCache.CallConstructor(obj);
			return obj;
		}
		catch (Exception inner) when (wrapExceptions)
		{
			throw new TargetInvocationException(inner);
		}
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	internal object CreateInstanceOfT()
	{
		ActivatorCache activatorCache = GenericCache as ActivatorCache;
		if (activatorCache == null)
		{
			activatorCache = (ActivatorCache)(GenericCache = new ActivatorCache(this));
		}
		if (!activatorCache.CtorIsPublic)
		{
			throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
		}
		object obj = activatorCache.CreateUninitializedObject(this);
		try
		{
			activatorCache.CallConstructor(obj);
			return obj;
		}
		catch (Exception inner)
		{
			throw new TargetInvocationException(inner);
		}
	}

	internal void InvalidateCachedNestedType()
	{
		Cache.InvalidateCachedNestedType();
	}

	internal bool IsGenericCOMObjectImpl()
	{
		return RuntimeTypeHandle.IsComObject(this, isGenericCOM: true);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object _CreateEnum(RuntimeType enumType, long value);

	internal static object CreateEnum(RuntimeType enumType, long value)
	{
		return _CreateEnum(enumType, value);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern object InvokeDispMethod(string name, BindingFlags invokeAttr, object target, object[] args, bool[] byrefModifiers, int culture, string[] namedParameters);

	[RequiresUnreferencedCode("The member might be removed")]
	private object ForwardCallToInvokeMember(string memberName, BindingFlags flags, object target, object[] aArgs, bool[] aArgsIsByRef, int[] aArgsWrapperTypes, Type[] aArgsTypes, Type retType)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		int num = aArgs.Length;
		ParameterModifier[] array = null;
		if (num > 0)
		{
			ParameterModifier parameterModifier = new ParameterModifier(num);
			for (int i = 0; i < num; i++)
			{
				parameterModifier[i] = aArgsIsByRef[i];
			}
			array = new ParameterModifier[1] { parameterModifier };
			if (aArgsWrapperTypes != null)
			{
				WrapArgsForInvokeCall(aArgs, aArgsWrapperTypes);
			}
		}
		flags |= BindingFlags.DoNotWrapExceptions;
		object obj = InvokeMember(memberName, flags, null, target, aArgs, array, null, null);
		for (int j = 0; j < num; j++)
		{
			if (array[0][j] && aArgs[j] != null)
			{
				Type type = aArgsTypes[j];
				if ((object)type != aArgs[j].GetType())
				{
					aArgs[j] = ForwardCallBinder.ChangeType(aArgs[j], type, null);
				}
			}
		}
		if (obj != null && (object)retType != obj.GetType())
		{
			obj = ForwardCallBinder.ChangeType(obj, retType, null);
		}
		return obj;
	}

	private static void WrapArgsForInvokeCall(object[] aArgs, int[] aArgsWrapperTypes)
	{
		int num = aArgs.Length;
		for (int i = 0; i < num; i++)
		{
			if (aArgsWrapperTypes[i] == 0)
			{
				continue;
			}
			if (((DispatchWrapperType)aArgsWrapperTypes[i]).HasFlag(DispatchWrapperType.SafeArray))
			{
				Type type = null;
				bool flag = false;
				switch ((DispatchWrapperType)(aArgsWrapperTypes[i] & -65537))
				{
				case DispatchWrapperType.Unknown:
					type = typeof(UnknownWrapper);
					break;
				case DispatchWrapperType.Dispatch:
					type = typeof(DispatchWrapper);
					break;
				case DispatchWrapperType.Error:
					type = typeof(ErrorWrapper);
					break;
				case DispatchWrapperType.Currency:
					type = typeof(CurrencyWrapper);
					break;
				case DispatchWrapperType.BStr:
					type = typeof(BStrWrapper);
					flag = true;
					break;
				}
				Array array = (Array)aArgs[i];
				int length = array.Length;
				object[] array2 = (object[])Array.CreateInstance(type, length);
				ConstructorInfo constructorInfo = ((!flag) ? type.GetConstructor(new Type[1] { typeof(object) }) : type.GetConstructor(new Type[1] { typeof(string) }));
				for (int j = 0; j < length; j++)
				{
					if (flag)
					{
						array2[j] = constructorInfo.Invoke(new object[1] { (string)array.GetValue(j) });
					}
					else
					{
						array2[j] = constructorInfo.Invoke(new object[1] { array.GetValue(j) });
					}
				}
				aArgs[i] = array2;
			}
			else
			{
				switch ((DispatchWrapperType)aArgsWrapperTypes[i])
				{
				case DispatchWrapperType.Unknown:
					aArgs[i] = new UnknownWrapper(aArgs[i]);
					break;
				case DispatchWrapperType.Dispatch:
					aArgs[i] = new DispatchWrapper(aArgs[i]);
					break;
				case DispatchWrapperType.Error:
					aArgs[i] = new ErrorWrapper(aArgs[i]);
					break;
				case DispatchWrapperType.Currency:
					aArgs[i] = new CurrencyWrapper(aArgs[i]);
					break;
				case DispatchWrapperType.BStr:
					aArgs[i] = new BStrWrapper((string)aArgs[i]);
					break;
				}
			}
		}
	}

	public object Clone()
	{
		return this;
	}

	public override bool Equals(object obj)
	{
		return obj == this;
	}

	public override int GetArrayRank()
	{
		if (!IsArrayImpl())
		{
			throw new ArgumentException(SR.Argument_HasToBeArrayClass);
		}
		return RuntimeTypeHandle.GetArrayRank(this);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return RuntimeTypeHandle.GetAttributes(this);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, ObjectType, inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, caType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)]
	public override MemberInfo[] GetDefaultMembers()
	{
		MemberInfo[] array = null;
		string defaultMemberName = GetDefaultMemberName();
		if (defaultMemberName != null)
		{
			array = GetMember(defaultMemberName);
		}
		return array ?? Array.Empty<MemberInfo>();
	}

	public override Type GetElementType()
	{
		return RuntimeTypeHandle.GetElementType(this);
	}

	public override string GetEnumName(object value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		RuntimeType runtimeType = (RuntimeType)value.GetType();
		if (!runtimeType.IsActualEnum && !Type.IsIntegerType(runtimeType))
		{
			throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, "value");
		}
		return Enum.GetName(this, Enum.ToUInt64(value));
	}

	private static void ThrowMustBeEnum()
	{
		throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
	}

	public override string[] GetEnumNames()
	{
		if (!IsActualEnum)
		{
			ThrowMustBeEnum();
		}
		string[] namesNoCopy = Enum.GetNamesNoCopy(this);
		return new ReadOnlySpan<string>(namesNoCopy).ToArray();
	}

	[RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use Enum.GetValues<T> or the GetEnumValuesAsUnderlyingType method instead.")]
	public override Array GetEnumValues()
	{
		if (!IsActualEnum)
		{
			ThrowMustBeEnum();
		}
		Array valuesAsUnderlyingTypeNoCopy = Enum.GetValuesAsUnderlyingTypeNoCopy(this);
		Array array = Array.CreateInstance(this, valuesAsUnderlyingTypeNoCopy.Length);
		Array.Copy(valuesAsUnderlyingTypeNoCopy, array, valuesAsUnderlyingTypeNoCopy.Length);
		return array;
	}

	public override Array GetEnumValuesAsUnderlyingType()
	{
		if (!IsActualEnum)
		{
			ThrowMustBeEnum();
		}
		return Enum.GetValuesAsUnderlyingType(this);
	}

	public override Type GetEnumUnderlyingType()
	{
		if (!IsActualEnum)
		{
			ThrowMustBeEnum();
		}
		return Enum.InternalGetUnderlyingType(this);
	}

	public override int GetHashCode()
	{
		return RuntimeHelpers.GetHashCode(this);
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return RuntimeTypeHandle.GetModule(this);
	}

	protected override TypeCode GetTypeCodeImpl()
	{
		TypeCode typeCode = Cache.TypeCode;
		if (typeCode != 0)
		{
			return typeCode;
		}
		typeCode = Type.GetRuntimeTypeCode(this);
		Cache.TypeCode = typeCode;
		return typeCode;
	}

	protected override bool HasElementTypeImpl()
	{
		return RuntimeTypeHandle.HasElementType(this);
	}

	protected override bool IsArrayImpl()
	{
		return RuntimeTypeHandle.IsArray(this);
	}

	protected override bool IsContextfulImpl()
	{
		return false;
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, caType, inherit);
	}

	public override bool IsEnumDefined(object value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		if (!IsActualEnum)
		{
			ThrowMustBeEnum();
		}
		RuntimeType runtimeType = (RuntimeType)value.GetType();
		if (runtimeType.IsActualEnum)
		{
			if (!runtimeType.IsEquivalentTo(this))
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, runtimeType, this));
			}
			runtimeType = (RuntimeType)runtimeType.GetEnumUnderlyingType();
		}
		if (runtimeType == StringType)
		{
			return Array.IndexOf(Enum.GetNamesNoCopy(this), (string)value) >= 0;
		}
		if (!Type.IsIntegerType(runtimeType))
		{
			throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
		}
		RuntimeType runtimeType2 = Enum.InternalGetUnderlyingType(this);
		if (runtimeType2 != runtimeType)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, runtimeType, runtimeType2));
		}
		switch (Type.GetTypeCode(runtimeType2))
		{
		case TypeCode.SByte:
			return Enum.IsDefinedPrimitive(this, (byte)(sbyte)value);
		case TypeCode.Byte:
			return Enum.IsDefinedPrimitive(this, (byte)value);
		case TypeCode.Int16:
			return Enum.IsDefinedPrimitive(this, (ushort)(short)value);
		case TypeCode.UInt16:
			return Enum.IsDefinedPrimitive(this, (ushort)value);
		case TypeCode.Int32:
			return Enum.IsDefinedPrimitive(this, (uint)(int)value);
		case TypeCode.UInt32:
			return Enum.IsDefinedPrimitive(this, (uint)value);
		case TypeCode.Int64:
			return Enum.IsDefinedPrimitive(this, (ulong)(long)value);
		case TypeCode.UInt64:
			return Enum.IsDefinedPrimitive(this, (ulong)value);
		case TypeCode.Single:
			return Enum.IsDefinedPrimitive(this, (float)value);
		case TypeCode.Double:
			return Enum.IsDefinedPrimitive(this, (double)value);
		case TypeCode.Char:
			return Enum.IsDefinedPrimitive(this, (char)value);
		default:
		{
			bool result;
			if (!(runtimeType2 == typeof(nint)))
			{
				if (!(runtimeType2 == typeof(nuint)))
				{
					throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
				}
				result = Enum.IsDefinedPrimitive<nuint>(this, (nuint)value);
			}
			else
			{
				result = Enum.IsDefinedPrimitive<nuint>(this, (nuint)(nint)value);
			}
			return result;
		}
		}
	}

	protected override bool IsByRefImpl()
	{
		return RuntimeTypeHandle.IsByRef(this);
	}

	protected override bool IsPrimitiveImpl()
	{
		return RuntimeTypeHandle.IsPrimitive(this);
	}

	protected override bool IsPointerImpl()
	{
		return RuntimeTypeHandle.IsPointer(this);
	}

	protected override bool IsCOMObjectImpl()
	{
		return RuntimeTypeHandle.IsComObject(this, isGenericCOM: false);
	}

	public override bool IsInstanceOfType([NotNullWhen(true)] object o)
	{
		return RuntimeTypeHandle.IsInstanceOfType(this, o);
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] Type c)
	{
		if ((object)c == null)
		{
			return false;
		}
		if ((object)c == this)
		{
			return true;
		}
		if (c.UnderlyingSystemType is RuntimeType type)
		{
			return RuntimeTypeHandle.CanCastTo(type, this);
		}
		if (c is TypeBuilder)
		{
			if (c.IsSubclassOf(this))
			{
				return true;
			}
			if (base.IsInterface)
			{
				return c.ImplementInterface(this);
			}
			if (IsGenericParameter)
			{
				Type[] genericParameterConstraints = GetGenericParameterConstraints();
				for (int i = 0; i < genericParameterConstraints.Length; i++)
				{
					if (!genericParameterConstraints[i].IsAssignableFrom(c))
					{
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags bindingFlags, Binder binder, object target, object[] providedArgs, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParams)
	{
		if (IsGenericParameter)
		{
			throw new InvalidOperationException(SR.Arg_GenericParameter);
		}
		if ((bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) == 0)
		{
			throw new ArgumentException(SR.Arg_NoAccessSpec, "bindingFlags");
		}
		if ((bindingFlags & (BindingFlags)255) == 0)
		{
			bindingFlags |= BindingFlags.Instance | BindingFlags.Public;
			if ((bindingFlags & BindingFlags.CreateInstance) == 0)
			{
				bindingFlags |= BindingFlags.Static;
			}
		}
		if (namedParams != null)
		{
			if (providedArgs != null)
			{
				if (namedParams.Length > providedArgs.Length)
				{
					throw new ArgumentException(SR.Arg_NamedParamTooBig, "namedParams");
				}
			}
			else if (namedParams.Length != 0)
			{
				throw new ArgumentException(SR.Arg_NamedParamTooBig, "namedParams");
			}
		}
		if (target != null && target.GetType().IsCOMObject)
		{
			if ((bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) == 0)
			{
				throw new ArgumentException(SR.Arg_COMAccess, "bindingFlags");
			}
			if ((bindingFlags & BindingFlags.GetProperty) != 0 && ((uint)(bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) & 0xFFFFEEFFu) != 0)
			{
				throw new ArgumentException(SR.Arg_PropSetGet, "bindingFlags");
			}
			if ((bindingFlags & BindingFlags.InvokeMethod) != 0 && ((uint)(bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) & 0xFFFFEEFFu) != 0)
			{
				throw new ArgumentException(SR.Arg_PropSetInvoke, "bindingFlags");
			}
			if ((bindingFlags & BindingFlags.SetProperty) != 0 && ((uint)(bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) & 0xFFFFDFFFu) != 0)
			{
				throw new ArgumentException(SR.Arg_COMPropSetPut, "bindingFlags");
			}
			if ((bindingFlags & BindingFlags.PutDispProperty) != 0 && ((uint)(bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) & 0xFFFFBFFFu) != 0)
			{
				throw new ArgumentException(SR.Arg_COMPropSetPut, "bindingFlags");
			}
			if ((bindingFlags & BindingFlags.PutRefDispProperty) != 0 && ((uint)(bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) & 0xFFFF7FFFu) != 0)
			{
				throw new ArgumentException(SR.Arg_COMPropSetPut, "bindingFlags");
			}
			ArgumentNullException.ThrowIfNull(name, "name");
			bool[] byrefModifiers = modifiers?[0].IsByRefArray;
			int culture2 = culture?.LCID ?? 1033;
			bool flag = (bindingFlags & BindingFlags.DoNotWrapExceptions) != 0;
			try
			{
				return InvokeDispMethod(name, bindingFlags, target, providedArgs, byrefModifiers, culture2, namedParams);
			}
			catch (TargetInvocationException ex) when (flag)
			{
				throw ex.InnerException;
			}
		}
		if (namedParams != null && Array.IndexOf(namedParams, null) >= 0)
		{
			throw new ArgumentException(SR.Arg_NamedParamNull, "namedParams");
		}
		int num = ((providedArgs != null) ? providedArgs.Length : 0);
		if (binder == null)
		{
			binder = Type.DefaultBinder;
		}
		if ((bindingFlags & BindingFlags.CreateInstance) != 0)
		{
			if ((bindingFlags & BindingFlags.CreateInstance) != 0 && (bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty)) != 0)
			{
				throw new ArgumentException(SR.Arg_CreatInstAccess, "bindingFlags");
			}
			return Activator.CreateInstance(this, bindingFlags, binder, providedArgs, culture);
		}
		if ((bindingFlags & (BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) != 0)
		{
			bindingFlags |= BindingFlags.SetProperty;
		}
		ArgumentNullException.ThrowIfNull(name, "name");
		if (name.Length == 0 || name.Equals("[DISPID=0]"))
		{
			name = GetDefaultMemberName() ?? "ToString";
		}
		bool flag2 = (bindingFlags & BindingFlags.GetField) != 0;
		bool flag3 = (bindingFlags & BindingFlags.SetField) != 0;
		if (flag2 || flag3)
		{
			if (flag2)
			{
				if (flag3)
				{
					throw new ArgumentException(SR.Arg_FldSetGet, "bindingFlags");
				}
				if ((bindingFlags & BindingFlags.SetProperty) != 0)
				{
					throw new ArgumentException(SR.Arg_FldGetPropSet, "bindingFlags");
				}
			}
			else
			{
				ArgumentNullException.ThrowIfNull(providedArgs, "providedArgs");
				if ((bindingFlags & BindingFlags.GetProperty) != 0)
				{
					throw new ArgumentException(SR.Arg_FldSetPropGet, "bindingFlags");
				}
				if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
				{
					throw new ArgumentException(SR.Arg_FldSetInvoke, "bindingFlags");
				}
			}
			FieldInfo fieldInfo = null;
			FieldInfo[] array = GetMember(name, MemberTypes.Field, bindingFlags) as FieldInfo[];
			if (array.Length == 1)
			{
				fieldInfo = array[0];
			}
			else if (array.Length != 0)
			{
				fieldInfo = binder.BindToField(bindingFlags, array, flag2 ? Empty.Value : providedArgs[0], culture);
			}
			if (fieldInfo != null)
			{
				if (fieldInfo.FieldType.IsArray || (object)fieldInfo.FieldType == typeof(Array))
				{
					int num2 = (((bindingFlags & BindingFlags.GetField) == 0) ? (num - 1) : num);
					if (num2 > 0)
					{
						int[] array2 = new int[num2];
						for (int i = 0; i < num2; i++)
						{
							try
							{
								array2[i] = ((IConvertible)providedArgs[i]).ToInt32(null);
							}
							catch (InvalidCastException)
							{
								throw new ArgumentException(SR.Arg_IndexMustBeInt);
							}
						}
						Array array3 = (Array)fieldInfo.GetValue(target);
						if ((bindingFlags & BindingFlags.GetField) != 0)
						{
							return array3.GetValue(array2);
						}
						array3.SetValue(providedArgs[num2], array2);
						return null;
					}
				}
				if (flag2)
				{
					if (num != 0)
					{
						throw new ArgumentException(SR.Arg_FldGetArgErr, "bindingFlags");
					}
					return fieldInfo.GetValue(target);
				}
				if (num != 1)
				{
					throw new ArgumentException(SR.Arg_FldSetArgErr, "bindingFlags");
				}
				fieldInfo.SetValue(target, providedArgs[0], bindingFlags, binder, culture);
				return null;
			}
			if ((bindingFlags & (BindingFlags)16773888) == 0)
			{
				throw new MissingFieldException(FullName, name);
			}
		}
		bool flag4 = (bindingFlags & BindingFlags.GetProperty) != 0;
		bool flag5 = (bindingFlags & BindingFlags.SetProperty) != 0;
		if (flag4 || flag5)
		{
			if (flag4)
			{
				if (flag5)
				{
					throw new ArgumentException(SR.Arg_PropSetGet, "bindingFlags");
				}
			}
			else if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
			{
				throw new ArgumentException(SR.Arg_PropSetInvoke, "bindingFlags");
			}
		}
		MethodInfo[] array4 = null;
		MethodInfo methodInfo = null;
		if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
		{
			MethodInfo[] array5 = GetMember(name, MemberTypes.Method, bindingFlags) as MethodInfo[];
			List<MethodInfo> list = null;
			foreach (MethodInfo methodInfo2 in array5)
			{
				if (!FilterApplyMethodInfo((RuntimeMethodInfo)methodInfo2, bindingFlags, CallingConventions.Any, new Type[num]))
				{
					continue;
				}
				if (methodInfo == null)
				{
					methodInfo = methodInfo2;
					continue;
				}
				if (list == null)
				{
					list = new List<MethodInfo>(array5.Length) { methodInfo };
				}
				list.Add(methodInfo2);
			}
			if (list != null)
			{
				array4 = list.ToArray();
			}
		}
		if ((methodInfo == null && flag4) || flag5)
		{
			PropertyInfo[] array6 = GetMember(name, MemberTypes.Property, bindingFlags) as PropertyInfo[];
			List<MethodInfo> list2 = null;
			for (int k = 0; k < array6.Length; k++)
			{
				MethodInfo methodInfo3 = null;
				methodInfo3 = ((!flag5) ? array6[k].GetGetMethod(nonPublic: true) : array6[k].GetSetMethod(nonPublic: true));
				if (methodInfo3 == null || !FilterApplyMethodInfo((RuntimeMethodInfo)methodInfo3, bindingFlags, CallingConventions.Any, new Type[num]))
				{
					continue;
				}
				if (methodInfo == null)
				{
					methodInfo = methodInfo3;
					continue;
				}
				if (list2 == null)
				{
					list2 = new List<MethodInfo>(array6.Length) { methodInfo };
				}
				list2.Add(methodInfo3);
			}
			if (list2 != null)
			{
				array4 = list2.ToArray();
			}
		}
		if (methodInfo != null)
		{
			if (array4 == null && num == 0 && methodInfo.GetParametersNoCopy().Length == 0 && (bindingFlags & BindingFlags.OptionalParamBinding) == 0)
			{
				return methodInfo.Invoke(target, bindingFlags, binder, providedArgs, culture);
			}
			if (array4 == null)
			{
				array4 = new MethodInfo[1] { methodInfo };
			}
			if (providedArgs == null)
			{
				providedArgs = Array.Empty<object>();
			}
			object state = null;
			MethodBase methodBase = null;
			try
			{
				Binder binder2 = binder;
				BindingFlags bindingAttr = bindingFlags;
				MethodBase[] match = array4;
				methodBase = binder2.BindToMethod(bindingAttr, match, ref providedArgs, modifiers, culture, namedParams, out state);
			}
			catch (MissingMethodException)
			{
			}
			if (methodBase == null)
			{
				throw new MissingMethodException(FullName, name);
			}
			object result = ((MethodInfo)methodBase).Invoke(target, bindingFlags, binder, providedArgs, culture);
			if (state != null)
			{
				binder.ReorderArgumentArray(ref providedArgs, state);
			}
			return result;
		}
		throw new MissingMethodException(FullName, name);
	}

	private RuntimeType GetBaseType()
	{
		if (base.IsInterface)
		{
			return null;
		}
		if (RuntimeTypeHandle.IsGenericVariable(this))
		{
			Type[] genericParameterConstraints = GetGenericParameterConstraints();
			RuntimeType runtimeType = ObjectType;
			for (int i = 0; i < genericParameterConstraints.Length; i++)
			{
				RuntimeType runtimeType2 = (RuntimeType)genericParameterConstraints[i];
				if (runtimeType2.IsInterface)
				{
					continue;
				}
				if (runtimeType2.IsGenericParameter)
				{
					GenericParameterAttributes genericParameterAttributes = runtimeType2.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
					if ((genericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) == 0 && (genericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
					{
						continue;
					}
				}
				runtimeType = runtimeType2;
			}
			if (runtimeType == ObjectType)
			{
				GenericParameterAttributes genericParameterAttributes2 = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
				if ((genericParameterAttributes2 & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
				{
					runtimeType = ValueType;
				}
			}
			return runtimeType;
		}
		return RuntimeTypeHandle.GetBaseType(this);
	}

	private static void ThrowIfTypeNeverValidGenericArgument(RuntimeType type)
	{
		if (type.IsPointer || type.IsFunctionPointer || type.IsByRef || type == typeof(void))
		{
			throw new ArgumentException(SR.Format(SR.Argument_NeverValidGenericArgument, type));
		}
	}

	internal static void SanityCheckGenericArguments(RuntimeType[] genericArguments, RuntimeType[] genericParameters)
	{
		ArgumentNullException.ThrowIfNull(genericArguments, "genericArguments");
		for (int i = 0; i < genericArguments.Length; i++)
		{
			ArgumentNullException.ThrowIfNull(genericArguments[i]);
			ThrowIfTypeNeverValidGenericArgument(genericArguments[i]);
		}
		if (genericArguments.Length != genericParameters.Length)
		{
			throw new ArgumentException(SR.Format(SR.Argument_NotEnoughGenArguments, genericArguments.Length, genericParameters.Length));
		}
	}

	internal static CorElementType GetUnderlyingType(RuntimeType type)
	{
		if (type.IsActualEnum)
		{
			type = (RuntimeType)Enum.GetUnderlyingType(type);
		}
		return RuntimeTypeHandle.GetCorElementType(type);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryGetByRefElementType(RuntimeType type, [NotNullWhen(true)] out RuntimeType elementType)
	{
		CorElementType corElementType = RuntimeTypeHandle.GetCorElementType(type);
		if (corElementType == CorElementType.ELEMENT_TYPE_BYREF)
		{
			elementType = RuntimeTypeHandle.GetElementType(type);
			return true;
		}
		elementType = null;
		return false;
	}

	internal bool CheckValue(ref object value)
	{
		if (IsInstanceOfType(value))
		{
			if (IsNullableOfT)
			{
				value = RuntimeMethodHandle.ReboxToNullable(value, this);
				return true;
			}
			return false;
		}
		bool copyBack = false;
		return TryChangeType(ref value, ref copyBack) switch
		{
			CheckValueStatus.Success => copyBack, 
			CheckValueStatus.ArgumentException => throw new ArgumentException(SR.Format(SR.Arg_ObjObjEx, value?.GetType(), this)), 
			CheckValueStatus.NotSupported_ByRefLike => throw new NotSupportedException(SR.NotSupported_ByRefLike), 
			_ => false, 
		};
	}

	internal bool CheckValue(ref object value, Binder binder, CultureInfo culture, BindingFlags invokeAttr)
	{
		if (IsInstanceOfType(value))
		{
			if (IsNullableOfT)
			{
				value = RuntimeMethodHandle.ReboxToNullable(value, this);
				return true;
			}
			return false;
		}
		bool copyBack = false;
		CheckValueStatus checkValueStatus = TryChangeType(ref value, ref copyBack);
		switch (checkValueStatus)
		{
		case CheckValueStatus.Success:
			return copyBack;
		case CheckValueStatus.ArgumentException:
			if ((invokeAttr & BindingFlags.ExactBinding) != 0 || binder == null || binder == Type.DefaultBinder)
			{
				break;
			}
			value = binder.ChangeType(value, this, culture);
			if (IsInstanceOfType(value))
			{
				if (IsNullableOfT)
				{
					value = RuntimeMethodHandle.ReboxToNullable(value, this);
				}
				return true;
			}
			checkValueStatus = TryChangeType(ref value, ref copyBack);
			if (checkValueStatus == CheckValueStatus.Success)
			{
				return copyBack;
			}
			break;
		}
		return checkValueStatus switch
		{
			CheckValueStatus.ArgumentException => throw new ArgumentException(SR.Format(SR.Arg_ObjObjEx, value?.GetType(), this)), 
			CheckValueStatus.NotSupported_ByRefLike => throw new NotSupportedException(SR.NotSupported_ByRefLike), 
			_ => false, 
		};
	}

	private CheckValueStatus TryChangeType(ref object value, ref bool copyBack)
	{
		if (TryGetByRefElementType(this, out var elementType))
		{
			copyBack = true;
			if (elementType.IsInstanceOfType(value))
			{
				if (RuntimeTypeHandle.IsValueType(elementType))
				{
					if (elementType.IsNullableOfT)
					{
						value = RuntimeMethodHandle.ReboxToNullable(value, elementType);
					}
					else
					{
						value = AllocateValueType(elementType, value);
					}
				}
				return CheckValueStatus.Success;
			}
			if (value == null)
			{
				if (!RuntimeTypeHandle.IsValueType(elementType))
				{
					return CheckValueStatus.Success;
				}
				if (elementType.IsByRefLike)
				{
					return CheckValueStatus.NotSupported_ByRefLike;
				}
				value = AllocateValueType(elementType, null);
				return CheckValueStatus.Success;
			}
			return CheckValueStatus.ArgumentException;
		}
		if (value == null)
		{
			if (base.IsPointer || IsFunctionPointer)
			{
				value = (nint)0;
				return CheckValueStatus.Success;
			}
			if (!RuntimeTypeHandle.IsValueType(this))
			{
				return CheckValueStatus.Success;
			}
			if (IsByRefLike)
			{
				return CheckValueStatus.NotSupported_ByRefLike;
			}
			value = AllocateValueType(this, null);
			return CheckValueStatus.Success;
		}
		if (base.IsPointer || IsEnum || base.IsPrimitive || IsFunctionPointer)
		{
			return TryChangeTypeSpecial(ref value);
		}
		return CheckValueStatus.ArgumentException;
	}
}

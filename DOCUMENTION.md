
C# Basics

Syntax
Variables: int x = 5; (declare and initialize a variable)
Data Types: int, string, bool, double, etc. (built-in data types)
Operators: +, -, *, /, ==, !=, etc. (arithmetic, comparison, logical, and assignment operators)
Control Flow: if, else, switch, while, for, foreach (conditional statements and loops)
Methods: void MyMethod() { } (declare a method)
Classes: public class MyClass { } (declare a class)
Data Types
Value Types:
int, long, float, double, bool, etc. (numeric and boolean types)
struct (user-defined value types)
Reference Types:
string (immutable string type)
class (user-defined reference types)
interface (abstract types)
Nullable Types: int?, bool?, etc. (nullable value types)
Operators
Arithmetic Operators: +, -, *, /, %, etc.
Comparison Operators: ==, !=, >, <, >=, <=
Logical Operators: &&, ||, !
Assignment Operators: =, +=, -=, *=, /=, etc.
Control Flow
Conditional Statements:
if statement: if (condition) { code }
if-else statement: if (condition) { code } else { code }
switch statement: switch (expression) { case value: code; break; }
Loops:
while loop: while (condition) { code }
for loop: for (init; condition; increment) { code }
foreach loop: foreach (var item in collection) { code }
Methods
Method Declaration: return-type MethodName(parameters) { code }
Method Invocation: MethodName(arguments)
Method Overloading: multiple methods with the same name but different parameters
Classes and Objects
Class Declaration: public class MyClass { }
Object Creation: MyClass obj = new MyClass();
Constructors: special methods called when an object is created
Properties: public int MyProperty { get; set; } (auto-implemented properties)
Fields: public int myField; (data members)
Methods: public void MyMethod() { } (instance methods)
Inheritance and Polymorphism
Inheritance: public class DerivedClass : BaseClass { }
Polymorphism: public void MyMethod() { } (method overriding)
Abstract Classes: public abstract class MyClass { }
Interfaces: public interface MyInterface { }
Exception Handling
Try-Catch Blocks: try { code } catch (Exception ex) { code }
Throwing Exceptions: throw new Exception("Error message");
Custom Exceptions: public class MyException : Exception { }
Generics
Generic Classes: public class MyClass<T> { }
Generic Methods: public void MyMethod<T>(T parameter) { }
Type Parameters: T, U, etc. (type parameters)
Delegates and Events
Delegates: public delegate void MyDelegate();
Events: public event MyDelegate MyEvent;
Event Handlers: public void MyEventHandler() { }
Lambda Expressions and LINQ
Lambda Expressions: (input) => { code }
LINQ: var result = from item in collection select item;
Async and Await
Async Methods: public async Task MyMethod() { }
Await: await Task.Delay(1000);
File Input/Output and Streams
File Operations: File.ReadAllText("file.txt");
Streams: `using (FileStream stream = new FileStream("file.txt", FileMode.Open)) { }


Here's an advanced C# documentation, covering more complex topics and features:

Advanced C# Topics

Dynamic Programming
Dynamic Keyword: dynamic myObject = GetDynamicObject();
DynamicObject Class: public class MyDynamicObject : DynamicObject { }
ExpandoObject Class: public class MyExpandoObject : ExpandoObject { }
Reflection
Type Class: Type myType = typeof(MyClass);
MethodInfo Class: MethodInfo myMethod = typeof(MyClass).GetMethod("MyMethod");
PropertyInfo Class: PropertyInfo myProperty = typeof(MyClass).GetProperty("MyProperty");
Reflection.Emit: AssemblyBuilder myAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly("MyAssembly", AssemblyBuilderAccess.Run);
Attributes
Attribute Classes: public class MyAttribute : Attribute { }
Attribute Targets: [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
Attribute Parameters: [MyAttribute("Parameter1", "Parameter2")]
Generics
Generic Constraints: public class MyClass<T> where T : class { }
Generic Type Parameters: public class MyClass<T, U> { }
Generic Methods: public void MyMethod<T>(T parameter) { }
Threading and Parallelism
Thread Class: Thread myThread = new Thread(MyMethod);
Task Parallel Library (TPL): Task myTask = Task.Run(MyMethod);
Parallel Class: Parallel.For(0, 10, MyMethod);
async and await: public async Task MyMethod() { await Task.Delay(1000); }
Regular Expressions
Regex Class: Regex myRegex = new Regex(@"\d+");
Match Class: Match myMatch = myRegex.Match("123abc");
Group Class: Group myGroup = myMatch.Groups[0];
XML and JSON
XmlDocument Class: XmlDocument myXml = new XmlDocument();
XElement Class: XElement myXml = XElement.Parse("<root><element>value</element></root>");
Json.NET: JsonConvert.SerializeObject(myObject);
Security
Cryptography: using (Aes aes = Aes.Create()) { }
SecureString Class: SecureString mySecureString = new SecureString();
PrincipalPermission Class: [PrincipalPermission(SecurityAction.Demand, Role = "MyRole")]
Interoperability
P/Invoke: [DllImport("mylibrary")] public static extern int MyMethod();
COM Interop: using (MyComObject myComObject = new MyComObject()) { }
Dynamic Language Runtime (DLR): using (ScriptEngine myEngine = new ScriptEngine("python")) { }
Advanced Language Features
Expression Trees: Expression<Func<int, int, int>> myExpression = (x, y) => x + y;
Query Expressions: var result = from item in collection select item;
Async Streams: IAsyncEnumerable<int> myStream = MyMethod();
Performance and Diagnostics
PerformanceCounter Class: PerformanceCounter myCounter = new PerformanceCounter("MyCategory", "MyCounter");
Stopwatch Class: Stopwatch myStopwatch = Stopwatch.StartNew();
Debugger Class: Debugger.Break();
Advanced Topics
Roslyn Compiler: using (var syntaxTree = CSharpSyntaxTree.ParseText("public class MyClass { }")) { }
CodeDOM: using (CodeCompileUnit myUnit = new CodeCompileUnit()) { }
Dynamic Method Invocation: MethodInfo myMethod = typeof(MyClass).GetMethod("MyMethod"); myMethod.Invoke(myObject, null);
This advanced C# documentation covers a wide range of topics, from dynamic programming and reflection to generics, threading, and parallelism. It also touches on regular expressions, XML and JSON, security, interoperability, and advanced language features. Additionally, it covers performance and diagnostics, as well as some advanced topics like Roslyn Compiler, CodeDOM, and dynamic method invocation.

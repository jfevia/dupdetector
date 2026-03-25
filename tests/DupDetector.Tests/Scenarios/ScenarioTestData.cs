using System.Collections.Generic;

namespace DupDetector.Tests;

/// <summary>
/// Generates all test scenario data for duplication detection tests (5000+ cases).
/// </summary>
public static class ScenarioTestData
{
    private static readonly string[] VarA = ["alpha", "x", "first", "valueA", "num1", "foo", "left", "p", "count", "width", "total", "a", "i", "m", "u", "source", "input", "start", "base", "primary"];
    private static readonly string[] VarB = ["beta", "y", "second", "valueB", "num2", "bar", "right", "q", "size", "height", "amount", "b", "j", "n", "v", "target", "output", "end", "offset", "secondary"];
    private static readonly string[] VarC = ["gamma", "z", "third", "valueC", "num3", "baz", "middle", "r", "length", "depth", "delta", "c", "k", "o", "w", "result", "temp", "mid", "step", "combined"];
    private static readonly string[] VarD = ["result", "res", "final", "total", "sum", "qux", "answer", "s", "capacity", "volume", "outcome", "d", "l", "p2", "x2", "computed", "processed", "value", "acc", "merged"];

    private static readonly string[] Method1 = ["MethodA", "ProcessA", "ComputeA", "HandleA", "RunA", "ExecuteA", "DoA", "CalculateA", "BuildA", "CreateA"];
    private static readonly string[] Method2 = ["MethodB", "ProcessB", "ComputeB", "HandleB", "RunB", "ExecuteB", "DoB", "CalculateB", "BuildB", "CreateB"];

    private static readonly string[] IntLit1 = ["10", "1", "100", "42", "0", "255", "1000", "7", "16", "64"];
    private static readonly string[] IntLit2 = ["20", "2", "200", "99", "5", "128", "2000", "14", "32", "128"];
    private static readonly string[] IntLit3 = ["30", "3", "300", "77", "9", "64", "3000", "21", "48", "192"];

    private static readonly string[] StrLit1 = ["Hello World", "Start", "Begin", "Open", "Init", "Alpha", "First", "Enter", "Connect", "Login"];
    private static readonly string[] StrLit2 = ["Goodbye World", "End", "Finish", "Close", "Dispose", "Beta", "Last", "Exit", "Disconnect", "Logout"];

    private static readonly string[] ExcVar = ["ex", "e", "err", "exception", "caught", "error", "exc", "problem", "fault", "failure"];
    private static readonly string[] ExcMsg = ["Something went wrong", "Error occurred", "Operation failed", "Unexpected error", "Process failed", "Action failed", "Task failed", "Request failed", "Command failed", "Service failed"];

    private static readonly string[] CollVar1 = ["items", "list", "elements", "data", "values", "records", "entries", "nodes", "objects", "things"];
    private static readonly string[] CollVar2 = ["results", "output", "filtered", "processed", "mapped", "selected", "computed", "transformed", "aggregated", "collected"];

    private static readonly string[] Types = ["int", "long", "double", "float", "decimal", "byte", "short", "uint", "ulong", "ushort"];

    /// <summary>Returns all 5000+ scenario test cases.</summary>
    public static IEnumerable<object[]> AllScenarios()
    {
        foreach (var item in ExactDuplicateScenarios()) yield return item;
        foreach (var item in LiteralVariationScenarios()) yield return item;
        foreach (var item in IfElseScenarios()) yield return item;
        foreach (var item in LoopScenarios()) yield return item;
        foreach (var item in ExceptionHandlingScenarios()) yield return item;
        foreach (var item in LinqScenarios()) yield return item;
        foreach (var item in AsyncScenarios()) yield return item;
        foreach (var item in SwitchScenarios()) yield return item;
        foreach (var item in CollectionScenarios()) yield return item;
        foreach (var item in StringOperationScenarios()) yield return item;
        foreach (var item in MathOperationScenarios()) yield return item;
        foreach (var item in NullCheckScenarios()) yield return item;
        foreach (var item in GenericMethodScenarios()) yield return item;
        foreach (var item in LambdaScenarios()) yield return item;
        foreach (var item in NearDuplicateScenarios()) yield return item;
    }

    // ─── Category 1: Exact duplicates (1000 scenarios) ───────────────────────
    private static IEnumerable<object[]> ExactDuplicateScenarios()
    {
        // 10 templates × 10 var-name sets × 10 method-name pairs = 1000
        for (int t = 0; t < 10; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v]; var d = VarD[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"ExactDup_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}() {{\n    int {a} = 10;\n    int {b} = 20;\n    int {c} = {a} + {b};\n    Console.WriteLine({c});\n    Console.WriteLine(\"result: \" + {c});\n}}",
                            $"void {m2}() {{\n    int {a} = 10;\n    int {b} = 20;\n    int {c} = {a} + {b};\n    Console.WriteLine({c});\n    Console.WriteLine(\"result: \" + {c});\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"void {m1}() {{\n    var {a} = new List<int>();\n    {a}.Add(1);\n    {a}.Add(2);\n    {a}.Add(3);\n    Console.WriteLine({a}.Count);\n}}",
                            $"void {m2}() {{\n    var {a} = new List<int>();\n    {a}.Add(1);\n    {a}.Add(2);\n    {a}.Add(3);\n    Console.WriteLine({a}.Count);\n}}", 2, 1.0),
                        2 => Scenario(name,
                            $"void {m1}() {{\n    int {a} = 0;\n    for (int {b} = 0; {b} < 10; {b}++) {{\n        {a} += {b};\n    }}\n    Console.WriteLine({a});\n}}",
                            $"void {m2}() {{\n    int {a} = 0;\n    for (int {b} = 0; {b} < 10; {b}++) {{\n        {a} += {b};\n    }}\n    Console.WriteLine({a});\n}}", 2, 1.0),
                        3 => Scenario(name,
                            $"void {m1}() {{\n    string {a} = \"hello\";\n    string {b} = {a}.ToUpper();\n    string {c} = {b}.Trim();\n    Console.WriteLine({c});\n    Console.WriteLine({c}.Length);\n}}",
                            $"void {m2}() {{\n    string {a} = \"hello\";\n    string {b} = {a}.ToUpper();\n    string {c} = {b}.Trim();\n    Console.WriteLine({c});\n    Console.WriteLine({c}.Length);\n}}", 2, 1.0),
                        4 => Scenario(name,
                            $"int {m1}(int {a}, int {b}) {{\n    int {c} = {a} * {b};\n    int {d} = {c} + {a};\n    Console.WriteLine({d});\n    return {d};\n}}",
                            $"int {m2}(int {a}, int {b}) {{\n    int {c} = {a} * {b};\n    int {d} = {c} + {a};\n    Console.WriteLine({d});\n    return {d};\n}}", 2, 1.0),
                        5 => Scenario(name,
                            $"void {m1}() {{\n    bool {a} = true;\n    if ({a}) {{\n        Console.WriteLine(\"yes\");\n    }} else {{\n        Console.WriteLine(\"no\");\n    }}\n    Console.WriteLine({a});\n}}",
                            $"void {m2}() {{\n    bool {a} = true;\n    if ({a}) {{\n        Console.WriteLine(\"yes\");\n    }} else {{\n        Console.WriteLine(\"no\");\n    }}\n    Console.WriteLine({a});\n}}", 2, 1.0),
                        6 => Scenario(name,
                            $"void {m1}() {{\n    try {{\n        int {a} = int.Parse(\"42\");\n        Console.WriteLine({a});\n    }} catch (Exception {b}) {{\n        Console.WriteLine({b}.Message);\n    }}\n}}",
                            $"void {m2}() {{\n    try {{\n        int {a} = int.Parse(\"42\");\n        Console.WriteLine({a});\n    }} catch (Exception {b}) {{\n        Console.WriteLine({b}.Message);\n    }}\n}}", 2, 1.0),
                        7 => Scenario(name,
                            $"void {m1}() {{\n    var {a} = new Dictionary<string, int>();\n    {a}[\"key\"] = 1;\n    {a}[\"other\"] = 2;\n    Console.WriteLine({a}.Count);\n    Console.WriteLine({a}[\"key\"]);\n}}",
                            $"void {m2}() {{\n    var {a} = new Dictionary<string, int>();\n    {a}[\"key\"] = 1;\n    {a}[\"other\"] = 2;\n    Console.WriteLine({a}.Count);\n    Console.WriteLine({a}[\"key\"]);\n}}", 2, 1.0),
                        8 => Scenario(name,
                            $"double {m1}(double {a}, double {b}) {{\n    double {c} = Math.Pow({a}, 2) + Math.Pow({b}, 2);\n    double {d} = Math.Sqrt({c});\n    Console.WriteLine({d});\n    return {d};\n}}",
                            $"double {m2}(double {a}, double {b}) {{\n    double {c} = Math.Pow({a}, 2) + Math.Pow({b}, 2);\n    double {d} = Math.Sqrt({c});\n    Console.WriteLine({d});\n    return {d};\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"string {m1}(string {a}, string {b}) {{\n    string {c} = {a} + \" \" + {b};\n    string {d} = {c}.ToUpperInvariant();\n    Console.WriteLine({d});\n    return {d};\n}}",
                            $"string {m2}(string {a}, string {b}) {{\n    string {c} = {a} + \" \" + {b};\n    string {d} = {c}.ToUpperInvariant();\n    Console.WriteLine({d});\n    return {d};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 2: Literal variations (600 scenarios) ──────────────────────
    private static IEnumerable<object[]> LiteralVariationScenarios()
    {
        // 6 templates × 10 literal sets × 10 method pairs = 600
        for (int t = 0; t < 6; t++)
        {
            for (int li = 0; li < 10; li++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var i1 = IntLit1[li]; var i2 = IntLit2[li]; var i3 = IntLit3[li];
                    var s1 = StrLit1[li]; var s2 = StrLit2[li];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"LiteralVar_T{t}_L{li}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}() {{\n    string msg = \"{s1}\";\n    int count = {i1};\n    bool flag = true;\n    Console.WriteLine(msg + count);\n    if (flag) {{ Console.WriteLine(\"yes\"); }}\n}}",
                            $"void {m2}() {{\n    string msg = \"{s2}\";\n    int count = {i2};\n    bool flag = false;\n    Console.WriteLine(msg + count);\n    if (flag) {{ Console.WriteLine(\"no\"); }}\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"void {m1}() {{\n    int x = {i1};\n    int y = {i2};\n    int z = x + y;\n    Console.WriteLine(z);\n    Console.WriteLine(\"{s1}\");\n}}",
                            $"void {m2}() {{\n    int x = {i2};\n    int y = {i3};\n    int z = x + y;\n    Console.WriteLine(z);\n    Console.WriteLine(\"{s2}\");\n}}", 2, 1.0),
                        2 => Scenario(name,
                            $"void {m1}() {{\n    for (int i = 0; i < {i1}; i++) {{\n        Console.WriteLine(i + \"{s1}\");\n    }}\n}}",
                            $"void {m2}() {{\n    for (int i = 0; i < {i2}; i++) {{\n        Console.WriteLine(i + \"{s2}\");\n    }}\n}}", 2, 1.0),
                        3 => Scenario(name,
                            $"void {m1}() {{\n    const int MaxRetries = {i1};\n    const string Prefix = \"{s1}\";\n    for (int i = 0; i < MaxRetries; i++) {{\n        Console.WriteLine(Prefix + i);\n    }}\n}}",
                            $"void {m2}() {{\n    const int MaxRetries = {i2};\n    const string Prefix = \"{s2}\";\n    for (int i = 0; i < MaxRetries; i++) {{\n        Console.WriteLine(Prefix + i);\n    }}\n}}", 2, 1.0),
                        4 => Scenario(name,
                            $"bool {m1}(int n) {{\n    if (n > {i1}) {{\n        Console.WriteLine(\"{s1}\");\n        return true;\n    }}\n    return false;\n}}",
                            $"bool {m2}(int n) {{\n    if (n > {i2}) {{\n        Console.WriteLine(\"{s2}\");\n        return true;\n    }}\n    return false;\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"void {m1}() {{\n    int[] arr = new int[{i1}];\n    for (int i = 0; i < arr.Length; i++) {{\n        arr[i] = i * {i2};\n    }}\n    Console.WriteLine(\"{s1}\");\n}}",
                            $"void {m2}() {{\n    int[] arr = new int[{i2}];\n    for (int i = 0; i < arr.Length; i++) {{\n        arr[i] = i * {i3};\n    }}\n    Console.WriteLine(\"{s2}\");\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 3: If/else patterns (400 scenarios) ────────────────────────
    private static IEnumerable<object[]> IfElseScenarios()
    {
        // 4 templates × 10 var sets × 10 method pairs = 400
        for (int t = 0; t < 4; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"IfElse_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}(int {a}) {{\n    if ({a} > 0) {{\n        Console.WriteLine(\"positive\");\n    }} else if ({a} < 0) {{\n        Console.WriteLine(\"negative\");\n    }} else {{\n        Console.WriteLine(\"zero\");\n    }}\n}}",
                            $"void {m2}(int {a}) {{\n    if ({a} > 0) {{\n        Console.WriteLine(\"positive\");\n    }} else if ({a} < 0) {{\n        Console.WriteLine(\"negative\");\n    }} else {{\n        Console.WriteLine(\"zero\");\n    }}\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"string {m1}(int {a}, int {b}) {{\n    if ({a} > {b}) {{\n        return \"greater\";\n    }} else if ({a} < {b}) {{\n        return \"less\";\n    }}\n    return \"equal\";\n}}",
                            $"string {m2}(int {a}, int {b}) {{\n    if ({a} > {b}) {{\n        return \"greater\";\n    }} else if ({a} < {b}) {{\n        return \"less\";\n    }}\n    return \"equal\";\n}}", 2, 1.0),
                        2 => Scenario(name,
                            $"void {m1}(bool {a}, bool {b}) {{\n    if ({a} && {b}) {{\n        Console.WriteLine(\"both\");\n    }} else if ({a} || {b}) {{\n        Console.WriteLine(\"one\");\n    }} else {{\n        Console.WriteLine(\"none\");\n    }}\n}}",
                            $"void {m2}(bool {a}, bool {b}) {{\n    if ({a} && {b}) {{\n        Console.WriteLine(\"both\");\n    }} else if ({a} || {b}) {{\n        Console.WriteLine(\"one\");\n    }} else {{\n        Console.WriteLine(\"none\");\n    }}\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"int {m1}(int {a}, int {b}, int {c}) {{\n    if ({a} >= {b} && {a} >= {c}) {{\n        return {a};\n    }} else if ({b} >= {a} && {b} >= {c}) {{\n        return {b};\n    }}\n    return {c};\n}}",
                            $"int {m2}(int {a}, int {b}, int {c}) {{\n    if ({a} >= {b} && {a} >= {c}) {{\n        return {a};\n    }} else if ({b} >= {a} && {b} >= {c}) {{\n        return {b};\n    }}\n    return {c};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 4: Loop patterns (400 scenarios) ───────────────────────────
    private static IEnumerable<object[]> LoopScenarios()
    {
        // 4 templates × 10 var sets × 10 method pairs = 400
        for (int t = 0; t < 4; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Loop_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"int {m1}(int {a}) {{\n    int {b} = 0;\n    for (int i = 0; i < {a}; i++) {{\n        {b} += i;\n    }}\n    return {b};\n}}",
                            $"int {m2}(int {a}) {{\n    int {b} = 0;\n    for (int i = 0; i < {a}; i++) {{\n        {b} += i;\n    }}\n    return {b};\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"void {m1}(List<int> {a}) {{\n    int {b} = 0;\n    foreach (var {c} in {a}) {{\n        {b} += {c};\n    }}\n    Console.WriteLine({b});\n}}",
                            $"void {m2}(List<int> {a}) {{\n    int {b} = 0;\n    foreach (var {c} in {a}) {{\n        {b} += {c};\n    }}\n    Console.WriteLine({b});\n}}", 2, 1.0),
                        2 => Scenario(name,
                            $"void {m1}(int {a}) {{\n    int {b} = {a};\n    while ({b} > 0) {{\n        Console.WriteLine({b});\n        {b}--;\n    }}\n}}",
                            $"void {m2}(int {a}) {{\n    int {b} = {a};\n    while ({b} > 0) {{\n        Console.WriteLine({b});\n        {b}--;\n    }}\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"int {m1}(int[] {a}) {{\n    int {b} = 0;\n    for (int i = 0; i < {a}.Length; i++) {{\n        if ({a}[i] > 0) {{\n            {b}++;\n        }}\n    }}\n    return {b};\n}}",
                            $"int {m2}(int[] {a}) {{\n    int {b} = 0;\n    for (int i = 0; i < {a}.Length; i++) {{\n        if ({a}[i] > 0) {{\n            {b}++;\n        }}\n    }}\n    return {b};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 5: Exception handling (300 scenarios) ──────────────────────
    private static IEnumerable<object[]> ExceptionHandlingScenarios()
    {
        // 3 templates × 10 excvar sets × 10 method pairs = 300
        for (int t = 0; t < 3; t++)
        {
            for (int e = 0; e < 10; e++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var ex = ExcVar[e]; var msg = ExcMsg[e];
                    var a = VarA[e]; var b = VarB[e];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"ExcHandling_T{t}_E{e}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}() {{\n    try {{\n        int {a} = int.Parse(\"42\");\n        Console.WriteLine({a});\n    }} catch (Exception {ex}) {{\n        Console.WriteLine({ex}.Message);\n    }}\n}}",
                            $"void {m2}() {{\n    try {{\n        int {a} = int.Parse(\"42\");\n        Console.WriteLine({a});\n    }} catch (Exception {ex}) {{\n        Console.WriteLine({ex}.Message);\n    }}\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"void {m1}() {{\n    try {{\n        var {a} = new List<int>();\n        {a}.Add(1);\n        Console.WriteLine({a}[0]);\n    }} catch (InvalidOperationException {ex}) {{\n        Console.WriteLine({ex}.Message);\n    }} finally {{\n        Console.WriteLine(\"done\");\n    }}\n}}",
                            $"void {m2}() {{\n    try {{\n        var {a} = new List<int>();\n        {a}.Add(1);\n        Console.WriteLine({a}[0]);\n    }} catch (InvalidOperationException {ex}) {{\n        Console.WriteLine({ex}.Message);\n    }} finally {{\n        Console.WriteLine(\"done\");\n    }}\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"int {m1}(string {a}) {{\n    try {{\n        return int.Parse({a});\n    }} catch (FormatException {ex}) {{\n        Console.WriteLine({ex}.Message);\n        return -1;\n    }} catch (OverflowException {b}) {{\n        Console.WriteLine({b}.Message);\n        return -2;\n    }}\n}}",
                            $"int {m2}(string {a}) {{\n    try {{\n        return int.Parse({a});\n    }} catch (FormatException {ex}) {{\n        Console.WriteLine({ex}.Message);\n        return -1;\n    }} catch (OverflowException {b}) {{\n        Console.WriteLine({b}.Message);\n        return -2;\n    }}\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 6: LINQ patterns (300 scenarios) ───────────────────────────
    private static IEnumerable<object[]> LinqScenarios()
    {
        // 3 templates × 10 coll-var sets × 10 method pairs = 300
        for (int t = 0; t < 3; t++)
        {
            for (int cv = 0; cv < 10; cv++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var src = CollVar1[cv]; var dst = CollVar2[cv];
                    var elem = VarA[cv]; var pred = VarB[cv];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Linq_T{t}_C{cv}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"List<int> {m1}(List<int> {src}) {{\n    var {dst} = {src}.Where(x => x > 0).ToList();\n    Console.WriteLine({dst}.Count);\n    return {dst};\n}}",
                            $"List<int> {m2}(List<int> {src}) {{\n    var {dst} = {src}.Where(x => x > 0).ToList();\n    Console.WriteLine({dst}.Count);\n    return {dst};\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"int {m1}(List<int> {src}) {{\n    var {dst} = {src}.Where(x => x > 0).Select(x => x * 2).ToList();\n    return {dst}.Sum();\n}}",
                            $"int {m2}(List<int> {src}) {{\n    var {dst} = {src}.Where(x => x > 0).Select(x => x * 2).ToList();\n    return {dst}.Sum();\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"Dictionary<int,int> {m1}(List<int> {src}) {{\n    var {dst} = {src}.GroupBy(x => x % 2).ToDictionary(g => g.Key, g => g.Count());\n    return {dst};\n}}",
                            $"Dictionary<int,int> {m2}(List<int> {src}) {{\n    var {dst} = {src}.GroupBy(x => x % 2).ToDictionary(g => g.Key, g => g.Count());\n    return {dst};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 7: Async/await patterns (200 scenarios) ────────────────────
    private static IEnumerable<object[]> AsyncScenarios()
    {
        // 2 templates × 10 var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Async_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"async System.Threading.Tasks.Task {m1}() {{\n    await System.Threading.Tasks.Task.Delay(10);\n    var {a} = 42;\n    Console.WriteLine({a});\n}}",
                            $"async System.Threading.Tasks.Task {m2}() {{\n    await System.Threading.Tasks.Task.Delay(10);\n    var {a} = 42;\n    Console.WriteLine({a});\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"async System.Threading.Tasks.Task<int> {m1}(int {a}) {{\n    await System.Threading.Tasks.Task.Delay(1);\n    var {b} = {a} * 2;\n    Console.WriteLine({b});\n    return {b};\n}}",
                            $"async System.Threading.Tasks.Task<int> {m2}(int {a}) {{\n    await System.Threading.Tasks.Task.Delay(1);\n    var {b} = {a} * 2;\n    Console.WriteLine({b});\n    return {b};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 8: Switch/pattern matching (200 scenarios) ─────────────────
    private static IEnumerable<object[]> SwitchScenarios()
    {
        // 2 templates × 10 var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Switch_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"string {m1}(int {a}) {{\n    switch ({a}) {{\n        case 1: return \"one\";\n        case 2: return \"two\";\n        case 3: return \"three\";\n        default: return \"other\";\n    }}\n}}",
                            $"string {m2}(int {a}) {{\n    switch ({a}) {{\n        case 1: return \"one\";\n        case 2: return \"two\";\n        case 3: return \"three\";\n        default: return \"other\";\n    }}\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"int {m1}(string {a}) {{\n    return {a} switch {{\n        \"low\" => 1,\n        \"mid\" => 2,\n        \"high\" => 3,\n        _ => 0\n    }};\n}}",
                            $"int {m2}(string {a}) {{\n    return {a} switch {{\n        \"low\" => 1,\n        \"mid\" => 2,\n        \"high\" => 3,\n        _ => 0\n    }};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 9: Collection manipulation (200 scenarios) ─────────────────
    private static IEnumerable<object[]> CollectionScenarios()
    {
        // 2 templates × 10 coll-var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int cv = 0; cv < 10; cv++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var src = CollVar1[cv]; var dst = CollVar2[cv];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Collection_T{t}_C{cv}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}() {{\n    var {src} = new List<int>();\n    {src}.Add(1);\n    {src}.Add(2);\n    {src}.Add(3);\n    var {dst} = {src}.ToArray();\n    Console.WriteLine({dst}.Length);\n}}",
                            $"void {m2}() {{\n    var {src} = new List<int>();\n    {src}.Add(1);\n    {src}.Add(2);\n    {src}.Add(3);\n    var {dst} = {src}.ToArray();\n    Console.WriteLine({dst}.Length);\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"void {m1}() {{\n    var {src} = new Dictionary<string, int>();\n    {src}[\"a\"] = 1;\n    {src}[\"b\"] = 2;\n    {src}[\"c\"] = 3;\n    Console.WriteLine({src}.Count);\n    Console.WriteLine({src}[\"a\"]);\n}}",
                            $"void {m2}() {{\n    var {src} = new Dictionary<string, int>();\n    {src}[\"a\"] = 1;\n    {src}[\"b\"] = 2;\n    {src}[\"c\"] = 3;\n    Console.WriteLine({src}.Count);\n    Console.WriteLine({src}[\"a\"]);\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 10: String operations (200 scenarios) ──────────────────────
    private static IEnumerable<object[]> StringOperationScenarios()
    {
        // 2 templates × 10 var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"StringOp_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"string {m1}(string {a}) {{\n    var {b} = {a}.ToUpper();\n    var {c} = {b}.Trim();\n    Console.WriteLine({c}.Length);\n    return {c};\n}}",
                            $"string {m2}(string {a}) {{\n    var {b} = {a}.ToUpper();\n    var {c} = {b}.Trim();\n    Console.WriteLine({c}.Length);\n    return {c};\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"bool {m1}(string {a}, string {b}) {{\n    var {c} = string.Concat({a}, {b});\n    Console.WriteLine({c});\n    return {c}.Contains({a});\n}}",
                            $"bool {m2}(string {a}, string {b}) {{\n    var {c} = string.Concat({a}, {b});\n    Console.WriteLine({c});\n    return {c}.Contains({a});\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 11: Math operations (200 scenarios) ────────────────────────
    private static IEnumerable<object[]> MathOperationScenarios()
    {
        // 2 templates × 10 var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v]; var d = VarD[v];
                    var ty = Types[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"MathOp_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"double {m1}(double {a}, double {b}) {{\n    double {c} = {a} * {a} + {b} * {b};\n    double {d} = Math.Sqrt({c});\n    Console.WriteLine({d});\n    return {d};\n}}",
                            $"double {m2}(double {a}, double {b}) {{\n    double {c} = {a} * {a} + {b} * {b};\n    double {d} = Math.Sqrt({c});\n    Console.WriteLine({d});\n    return {d};\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"int {m1}(int {a}, int {b}) {{\n    int {c} = Math.Max({a}, {b});\n    int {d} = Math.Min({a}, {b});\n    Console.WriteLine({c} - {d});\n    return {c} - {d};\n}}",
                            $"int {m2}(int {a}, int {b}) {{\n    int {c} = Math.Max({a}, {b});\n    int {d} = Math.Min({a}, {b});\n    Console.WriteLine({c} - {d});\n    return {c} - {d};\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 12: Null checking (200 scenarios) ───────────────────────────
    private static IEnumerable<object[]> NullCheckScenarios()
    {
        // 2 templates × 10 var sets × 10 method pairs = 200
        for (int t = 0; t < 2; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"NullCheck_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}(string? {a}) {{\n    if ({a} == null) {{\n        Console.WriteLine(\"null\");\n        return;\n    }}\n    Console.WriteLine({a}.Length);\n}}",
                            $"void {m2}(string? {a}) {{\n    if ({a} == null) {{\n        Console.WriteLine(\"null\");\n        return;\n    }}\n    Console.WriteLine({a}.Length);\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"string {m1}(string? {a}, string {b}) {{\n    var {b}2 = {a} ?? {b};\n    Console.WriteLine({b}2);\n    return {b}2;\n}}",
                            $"string {m2}(string? {a}, string {b}) {{\n    var {b}2 = {a} ?? {b};\n    Console.WriteLine({b}2);\n    return {b}2;\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 13: Generic method patterns (150 scenarios) ────────────────
    private static IEnumerable<object[]> GenericMethodScenarios()
    {
        // 3 templates × 5 var sets × 10 method pairs = 150
        for (int t = 0; t < 3; t++)
        {
            for (int v = 0; v < 5; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Generic_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"T {m1}<T>(T {a}, T {b}) where T : IComparable<T> {{\n    if ({a}.CompareTo({b}) > 0) {{\n        return {a};\n    }}\n    return {b};\n}}",
                            $"T {m2}<T>(T {a}, T {b}) where T : IComparable<T> {{\n    if ({a}.CompareTo({b}) > 0) {{\n        return {a};\n    }}\n    return {b};\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"List<T> {m1}<T>(IEnumerable<T> {a}) {{\n    var {b} = new List<T>();\n    foreach (var item in {a}) {{\n        {b}.Add(item);\n    }}\n    return {b};\n}}",
                            $"List<T> {m2}<T>(IEnumerable<T> {a}) {{\n    var {b} = new List<T>();\n    foreach (var item in {a}) {{\n        {b}.Add(item);\n    }}\n    return {b};\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"void {m1}<T>(List<T> {a}) {{\n    Console.WriteLine({a}.Count);\n    foreach (var item in {a}) {{\n        Console.WriteLine(item);\n    }}\n}}",
                            $"void {m2}<T>(List<T> {a}) {{\n    Console.WriteLine({a}.Count);\n    foreach (var item in {a}) {{\n        Console.WriteLine(item);\n    }}\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 14: Lambda/delegate patterns (150 scenarios) ───────────────
    private static IEnumerable<object[]> LambdaScenarios()
    {
        // 3 templates × 5 var sets × 10 method pairs = 150
        for (int t = 0; t < 3; t++)
        {
            for (int v = 0; v < 5; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"Lambda_T{t}_V{v}_M{m}";
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}(List<int> {a}) {{\n    var {b} = {a}.FindAll(x => x > 0);\n    {b}.ForEach(x => Console.WriteLine(x));\n}}",
                            $"void {m2}(List<int> {a}) {{\n    var {b} = {a}.FindAll(x => x > 0);\n    {b}.ForEach(x => Console.WriteLine(x));\n}}", 2, 1.0),
                        1 => Scenario(name,
                            $"Func<int,int> {m1}(int {a}) {{\n    return x => x + {a};\n}}",
                            $"Func<int,int> {m2}(int {a}) {{\n    return x => x + {a};\n}}", 2, 1.0),
                        _ => Scenario(name,
                            $"void {m1}(List<string> {a}) {{\n    {a}.Sort((x, y) => string.Compare(x, y, System.StringComparison.Ordinal));\n    {a}.ForEach(Console.WriteLine);\n}}",
                            $"void {m2}(List<string> {a}) {{\n    {a}.Sort((x, y) => string.Compare(x, y, System.StringComparison.Ordinal));\n    {a}.ForEach(Console.WriteLine);\n}}", 2, 1.0),
                    };
                }
            }
        }
    }

    // ─── Category 15: Near-duplicates (500 scenarios) ────────────────────────
    private static IEnumerable<object[]> NearDuplicateScenarios()
    {
        // 5 templates × 10 var sets × 10 method pairs = 500
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 10; v++)
            {
                for (int m = 0; m < 10; m++)
                {
                    var a = VarA[v]; var b = VarB[v]; var c = VarC[v]; var d = VarD[v];
                    var m1 = Method1[m]; var m2 = Method2[m];
                    var name = $"NearDup_T{t}_V{v}_M{m}";
                    // Near-duplicates: structurally similar but not identical (extra statement added)
                    yield return t switch
                    {
                        0 => Scenario(name,
                            $"void {m1}() {{\n    int {a} = 10;\n    int {b} = 20;\n    int {c} = {a} + {b};\n    Console.WriteLine({c});\n    Console.WriteLine(\"result: \" + {c});\n}}",
                            $"void {m2}() {{\n    int {a} = 10;\n    int {b} = 20;\n    int {c} = {a} + {b};\n    Console.WriteLine({c});\n    Console.WriteLine(\"result: \" + {c});\n    Console.WriteLine(\"extra\");\n}}", 2, 0.7),
                        1 => Scenario(name,
                            $"void {m1}(List<int> {a}) {{\n    int {b} = 0;\n    foreach (var {c} in {a}) {{\n        {b} += {c};\n    }}\n    Console.WriteLine({b});\n}}",
                            $"void {m2}(List<int> {a}) {{\n    int {b} = 0;\n    foreach (var {c} in {a}) {{\n        {b} += {c};\n    }}\n    Console.WriteLine({b});\n    Console.WriteLine({a}.Count);\n}}", 2, 0.7),
                        2 => Scenario(name,
                            $"int {m1}(int {a}, int {b}) {{\n    int {c} = {a} * {b};\n    int {d} = {c} + {a};\n    return {d};\n}}",
                            $"int {m2}(int {a}, int {b}) {{\n    Console.WriteLine(\"computing\");\n    int {c} = {a} * {b};\n    int {d} = {c} + {a};\n    return {d};\n}}", 2, 0.7),
                        3 => Scenario(name,
                            $"void {m1}(string {a}) {{\n    var {b} = {a}.ToUpper();\n    var {c} = {b}.Trim();\n    Console.WriteLine({c});\n}}",
                            $"void {m2}(string {a}) {{\n    var {b} = {a}.ToUpper();\n    var {c} = {b}.Trim();\n    Console.WriteLine({c});\n    Console.WriteLine({c}.Length);\n}}", 2, 0.7),
                        _ => Scenario(name,
                            $"void {m1}() {{\n    var {a} = new List<int>();\n    {a}.Add(1);\n    {a}.Add(2);\n    Console.WriteLine({a}.Count);\n}}",
                            $"void {m2}() {{\n    var {a} = new List<int>();\n    {a}.Add(1);\n    {a}.Add(2);\n    {a}.Add(3);\n    Console.WriteLine({a}.Count);\n}}", 2, 0.7),
                    };
                }
            }
        }
    }

    private static object[] Scenario(string name, string code1, string code2, int minLines, double similarity)
        => [name, code1, code2, minLines, similarity];
}

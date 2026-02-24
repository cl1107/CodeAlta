// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CodeNoesis.CodexSdk;

namespace CodeNoesis.Tests;

[TestClass]
public class CodexProcessTests
{
    [TestMethod]
    public void ParseFnmMultishellPath_ExtractsPath()
    {
        const string output = """
            SET PATH=C:\Users\test\.cache\fnm_multishells\1234_5678;C:\Windows
            SET FNM_MULTISHELL_PATH=C:\Users\test\.cache\fnm_multishells\1234_5678
            SET FNM_VERSION_FILE_STRATEGY=local
            SET FNM_DIR=C:\Users\test\AppData\Roaming\fnm
            """;

        var result = CodexProcess.ParseFnmMultishellPath(output);

        Assert.AreEqual(@"C:\Users\test\.cache\fnm_multishells\1234_5678", result);
    }

    [TestMethod]
    public void ParseFnmMultishellPath_ReturnsNull_WhenVariableMissing()
    {
        const string output = """
            SET PATH=C:\Windows
            SET FNM_DIR=C:\Users\test\AppData\Roaming\fnm
            """;

        var result = CodexProcess.ParseFnmMultishellPath(output);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseFnmMultishellPath_ReturnsNull_WhenOutputEmpty()
    {
        var result = CodexProcess.ParseFnmMultishellPath("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseFnmMultishellPath_IsCaseInsensitive()
    {
        const string output = "set fnm_multishell_path=/home/user/.cache/fnm/1234\n";

        var result = CodexProcess.ParseFnmMultishellPath(output);

        Assert.AreEqual("/home/user/.cache/fnm/1234", result);
    }

    [TestMethod]
    public void FindExecutable_ReturnsNull_ForNonexistentBinary()
    {
        var result = CodexProcess.FindExecutable("this_binary_does_not_exist_12345");

        Assert.IsNull(result);
    }
}

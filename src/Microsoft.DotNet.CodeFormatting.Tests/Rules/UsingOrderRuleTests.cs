// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DotNet.CodeFormatting.Tests
{
    public sealed class UsingOrderRuleTests : SyntaxRuleTestBase
    {
        internal override ISyntaxFormattingRule Rule
        {
            get { return new Rules.UsingOrderRule(); }
        }

        [Fact]
        public void Basic()
        {
            var source = @"
using NS2;
using System.IO;
using NS2.Text;
using System;

namespace NS1
{
    class C1 { }
}";

            var expected = @"using System;
using System.IO;

using NS2;
using NS2.Text;

namespace NS1
{
    class C1 { }
}";
            Verify(source, expected);
        }

        [Fact]
        public void InsideNamespaceUnaffected()
        {
            var source = @"
namespace NS2
{
    using NS4;
    using NS3;
    class C1 { }
}";

            Verify(source, source);
        }

        [Fact]
        public void SimpleMoveWithComment()
        {
            var source = @"
// test
using NS1.Internal;
using NS1;

namespace NS2
{

    class C1 { }
}";

            var expected = @"using NS1;
// test
using NS1.Internal;

namespace NS2
{

    class C1 { }
}";

            Verify(source, expected);
        }
    }
}

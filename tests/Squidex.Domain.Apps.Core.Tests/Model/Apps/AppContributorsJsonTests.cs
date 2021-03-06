﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Squidex.Domain.Apps.Core.Apps;
using Xunit;

namespace Squidex.Domain.Apps.Core.Model.Apps
{
    public class AppContributorsJsonTests
    {
        private readonly JsonSerializer serializer = TestData.DefaultSerializer();

        [Fact]
        public void Should_serialize_and_deserialize()
        {
            var contributors = AppContributors.Empty;

            contributors = contributors.Assign("1", Role.Developer);
            contributors = contributors.Assign("2", Role.Editor);
            contributors = contributors.Assign("3", Role.Owner);

            var serialized = JToken.FromObject(contributors, serializer).ToObject<AppContributors>(serializer);

            serialized.Should().BeEquivalentTo(contributors);
        }
    }
}

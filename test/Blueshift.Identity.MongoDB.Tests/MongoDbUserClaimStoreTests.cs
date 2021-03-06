﻿using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Blueshift.Identity.MongoDB.Tests
{
    public class MongoDbUserClaimStoreTests : MongoDbIdentityStoreTestBase
    {
        private const string Claim1Type = nameof(Claim1Type);
        private const string Claim1Value = nameof(Claim1Value);
        private const string Claim2Type = nameof(Claim2Type);
        private const string Claim2Value = nameof(Claim2Value);
        private const string Claim3Type = nameof(Claim3Type);
        private const string Claim3Value = nameof(Claim3Value);

        private static readonly Claim Claim1 = new Claim(Claim1Type, Claim1Value);
        private static readonly Claim Claim2 = new Claim(Claim2Type, Claim2Value);
        private static readonly Claim Claim3 = new Claim(Claim3Type, Claim3Value);

        private readonly IUserClaimStore<MongoDbIdentityUser> _mongoDbUserClaimStore;

        public MongoDbUserClaimStoreTests(MongoDbIdentityFixture mongoDbIdentityFixture)
            : base(mongoDbIdentityFixture)
        {
            _mongoDbUserClaimStore = mongoDbIdentityFixture.GetService<IUserClaimStore<MongoDbIdentityUser>>();
        }

        protected override MongoDbIdentityUser CreateUser()
        {
            var user = base.CreateUser();
            user.Claims.Add(new MongoDbIdentityClaim(Claim1));
            user.Claims.Add(new MongoDbIdentityClaim(Claim2));
            return user;
        }

        [Fact]
        public async void Can_add_claim_async()
        {
            var user = CreateUser();
            await _mongoDbUserClaimStore.AddClaimsAsync(user, new[] { Claim3 }, new CancellationToken());
            var newClaims = new [] { Claim1, Claim2, Claim3 };
            Assert.Equal(newClaims, user.Claims.Select(claim => claim.ToClaim()), new ClaimComparer());
        }

        [Fact]
        public async void Can_get_claims_async()
        {
            var user = CreateUser();
            var claims = new [] { Claim1, Claim2 };
            Assert.Equal(claims, await _mongoDbUserClaimStore.GetClaimsAsync(user, new CancellationToken()), new ClaimComparer());
        }

        [Fact]
        public async void Can_get_users_for_claim_async()
        {
            var user = await CreateUserInDatabase();
            Assert.Equal(user, (await _mongoDbUserClaimStore.GetUsersForClaimAsync(Claim1, new CancellationToken())).Single(), new MongoDbIdentityUserComparer());
            Assert.Equal(user, (await _mongoDbUserClaimStore.GetUsersForClaimAsync(Claim2, new CancellationToken())).Single(), new MongoDbIdentityUserComparer());
            Assert.Empty(await _mongoDbUserClaimStore.GetUsersForClaimAsync(Claim3, new CancellationToken()));
        }

        [Fact]
        public async void Can_remove_claims_async()
        {
            var user = CreateUser();
            var claimsToRemove = new [] { Claim1, Claim2 };
            await _mongoDbUserClaimStore.RemoveClaimsAsync(user, claimsToRemove, new CancellationToken());
            Assert.Empty(user.Claims);
        }

        [Fact]
        public async void Can_replace_claims_async()
        {
            var user = CreateUser();
            var newClaims = new [] { Claim3, Claim2 };
            await _mongoDbUserClaimStore.ReplaceClaimAsync(user, Claim1, Claim3, new CancellationToken());
            Assert.Equal(newClaims, user.Claims.Select(claim => claim.ToClaim()), new ClaimComparer());
        }
    }
}
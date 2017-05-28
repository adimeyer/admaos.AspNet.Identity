using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Raven.Tests.Helpers;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Exceptions;
using Raven.Client.UniqueConstraints;

// ReSharper disable AccessToDisposedClosure

namespace admaos.AspNet.Identity.RavenDB.Tests
{
    [TestFixture]
    public class UserStoreTests : RavenTestBase
    {
        private IDocumentStore _store;

        private UserStore<IdentityUser> GetNewUserStore()
        {
            return GetNewUserStore(_store.OpenAsyncSession());
        }

        private UserStore<IdentityUser> GetNewUserStore(IAsyncDocumentSession session)
        {
            return new UserStore<IdentityUser>(session);
        }

        private static async Task<T> ThrowsAsync<T>(Func<Task> testCode)
            where T : Exception
        {
            try
            {
                await testCode();
                Assert.Throws<T>(() => { }); // Use xUnit's default behavior.
            }
            catch (T exception)
            {
                return exception;
            }
            // Never reached. Compiler doesn't know Assert.Throws above always throws.
            return null;
        }

        [SetUp]
        public void SetUp()
        {
            _store = NewDocumentStore(noStaleQueries: true);
            ((EmbeddableDocumentStore) _store).RegisterListener(new UniqueConstraintsStoreListener());
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        //[Test]
        //public void ThrowIfDisposed_IsDisposed_Throws()
        //{
        //    using (var userStore = GetNewUserStore())
        //    {
        //        userStore.Dispose();
        //        Assert.Throws<ObjectDisposedException>(() => userStore.ThrowIfDisposed());
        //    }
        //}

        // Test ensures that the identity user can be saved to and read from RavenDb
        // Added this test after noticing an issue with classes that have more than one non-default constructor (System.Security.Claims.Claim)
        [Test]
        public async Task UserStore_SaveAndLoadIdentityUserInRavenDb_IsSuccessful()
        {
            var email = "UserStore_SaveAndLoadIdentityUserInRavenDb_IsSuccessful@admaos.ch";
            var emailConfirmed = true;
            var passwordHash = "aa0030489opja-sdölf0'p928u";
            var securityStamp = "a09ui3lkjer09yudfla";
            var phoneNumber = "+41 41 123 45 67";
            var phoneNumberConfirmed = true;
            var twoFactorEnabled = true;
            var lockoutEndDateUtc = new DateTime(2016, 9, 6);
            var lockoutEnabled = true;
            var accessFailedCount = 1;
            var roles = new List<string> {"admin", "accountant"};
            var claims = new List<Claim> {new Claim("test1", "test2")};
            var logins = new List<UserLoginInfo> {new UserLoginInfo("loginProvider1", "providerKey1")};
            var userName = email;

            var usr1 = new IdentityUser
            {
                Email = email,
                EmailConfirmed = emailConfirmed,
                PasswordHash = passwordHash,
                SecurityStamp = securityStamp,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = phoneNumberConfirmed,
                TwoFactorEnabled = twoFactorEnabled,
                LockoutEndDateUtc = lockoutEndDateUtc,
                LockoutEnabled = lockoutEnabled,
                AccessFailedCount = accessFailedCount,
                Roles = roles,
                Claims = claims,
                Logins = logins,
                UserName = userName
            };

            _store.Conventions.CustomizeJsonSerializer +=
                serializer => serializer.Converters.Add(new ClaimJsonConverter());

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            IdentityUser usr2;
            using (var sess = _store.OpenAsyncSession())
            {
                usr2 = await sess.LoadAsync<IdentityUser>(usr1.Id).ConfigureAwait(false);
            }

            Assert.AreEqual(usr1.Id, usr2.Id);

            Assert.AreEqual(email, usr1.Email);
            Assert.AreEqual(email, usr2.Email);

            Assert.AreEqual(emailConfirmed, usr1.EmailConfirmed);
            Assert.AreEqual(emailConfirmed, usr2.EmailConfirmed);

            Assert.AreEqual(passwordHash, usr1.PasswordHash);
            Assert.AreEqual(passwordHash, usr2.PasswordHash);

            Assert.AreEqual(securityStamp, usr1.SecurityStamp);
            Assert.AreEqual(securityStamp, usr2.SecurityStamp);

            Assert.AreEqual(phoneNumber, usr1.PhoneNumber);
            Assert.AreEqual(phoneNumber, usr2.PhoneNumber);

            Assert.AreEqual(phoneNumberConfirmed, usr1.PhoneNumberConfirmed);
            Assert.AreEqual(phoneNumberConfirmed, usr2.PhoneNumberConfirmed);

            Assert.AreEqual(twoFactorEnabled, usr1.TwoFactorEnabled);
            Assert.AreEqual(twoFactorEnabled, usr2.TwoFactorEnabled);

            Assert.AreEqual(lockoutEndDateUtc, usr1.LockoutEndDateUtc);
            Assert.AreEqual(lockoutEndDateUtc, usr2.LockoutEndDateUtc);

            Assert.AreEqual(lockoutEnabled, usr1.LockoutEnabled);
            Assert.AreEqual(lockoutEnabled, usr2.LockoutEnabled);

            Assert.AreEqual(accessFailedCount, usr1.AccessFailedCount);
            Assert.AreEqual(accessFailedCount, usr2.AccessFailedCount);

            Assert.AreEqual(roles, usr1.Roles);
            Assert.AreEqual(roles, usr2.Roles);

            Assert.AreEqual(claims.Count, usr1.Claims.Count);
            Assert.AreEqual(claims.Count, usr2.Claims.Count);
            for (int i = 0; i < claims.Count; i++)
            {
                Assert.AreEqual(claims[i].Type, usr1.Claims[i].Type);
                Assert.AreEqual(claims[i].Type, usr2.Claims[i].Type);
                Assert.AreEqual(claims[i].Value, usr1.Claims[i].Value);
                Assert.AreEqual(claims[i].Value, usr2.Claims[i].Value);
            }

            Assert.AreEqual(logins.Count, usr1.Logins.Count);
            Assert.AreEqual(logins.Count, usr2.Logins.Count);
            for (int i = 0; i < claims.Count; i++)
            {
                Assert.AreEqual(logins[i].LoginProvider, usr1.Logins[i].LoginProvider);
                Assert.AreEqual(logins[i].LoginProvider, usr2.Logins[i].LoginProvider);
                Assert.AreEqual(logins[i].ProviderKey, usr1.Logins[i].ProviderKey);
                Assert.AreEqual(logins[i].ProviderKey, usr2.Logins[i].ProviderKey);
            }

            Assert.AreEqual(userName, usr1.UserName);
            Assert.AreEqual(userName, usr2.UserName);
        }

        [Test]
        public void UserStore_SessionParameterNull_Throws()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws(typeof(ArgumentNullException), () => new UserStore<IdentityUser>(null));
        }

        [Test]
        public void UserStore_SessionWithoutUniqueConstraingListener_Throws()
        {
            using (var store = NewDocumentStore())
            {
                using (var sess = store.OpenAsyncSession())
                {
                    // ReSharper disable once ObjectCreationAsStatement
                    Assert.Throws(typeof (InvalidOperationException), () => new UserStore<IdentityUser>(sess));
                }
            }
        }

        [Test]
        public async Task CreateAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.CreateAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CreateAsync_NewUser_IsSuccessful()
        {
            var usr1 = new IdentityUser {UserName = "CreateAsync_CreateNewUser_IsSuccessful" };
            IdentityUser usr2;

            using (var sess = _store.OpenAsyncSession())
            {
                using (var userStore = GetNewUserStore(sess))
                {
                    await userStore.CreateAsync(usr1).ConfigureAwait(false);
                }
                usr2 = await sess.LoadAsync<IdentityUser>(usr1.Id).ConfigureAwait(false);
            }

            Assert.AreSame(usr1, usr2);
        }

        [Test]
        public async Task UpdateAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.UpdateAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task UpdateAsync_UpdateUser_IsSuccessful()
        {
            const string userName = "UpdateAsync_UpdateUser_IsSuccessful";
            var usr1 = new IdentityUser {UserName = userName};
            IdentityUser usr2;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var sess = _store.OpenAsyncSession())
            {
                usr1 = await sess.LoadAsync<IdentityUser>(usr1.Id).ConfigureAwait(false);
                using (var userStore = GetNewUserStore(sess))
                {
                    usr1.Email = userName + "2";
                    await userStore.UpdateAsync(usr1);
                }
            }

            using (var sess = _store.OpenAsyncSession())
            {
                usr2 = await sess.LoadAsync<IdentityUser>(usr1.Id).ConfigureAwait(false);
            }

            Assert.AreEqual(usr1.Email, usr2.Email);
        }

        [Test]
        public async Task DeleteAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.DeleteAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task DeleteAsync_RemoveUser_IsSuccessful()
        {
            var usr1 = new IdentityUser { UserName = "DeleteAsync_RemoveUser_IsSuccessful" };
            int usrCount;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var sess = _store.OpenAsyncSession())
            {
                usr1 = await sess.LoadAsync<IdentityUser>(usr1.Id).ConfigureAwait(false);
                using (var userStore = GetNewUserStore(sess))
                {
                    await userStore.DeleteAsync(usr1);
                }
            }

            using (var sess = _store.OpenAsyncSession())
            {
                WaitForIndexing(_store);
                usrCount = await sess.Query<IdentityUser>().CountAsync().ConfigureAwait(false);
            }

            Assert.AreEqual(0, usrCount);
        }

        [Test]
        public async Task FindByIdAsync_FindUser_IsSuccessful()
        {
            var usr1 = new IdentityUser { UserName = "FindByIdAsync_FindUser_IsSuccessful" };
            usr1.Claims.Add(new Claim("test", "test"));
            IdentityUser usr2;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                usr2 = await userStore.FindByIdAsync(usr1.Id).ConfigureAwait(false);
            }

            Assert.IsNotNull(usr2);
        }

        [Test]
        public async Task FindByNameAsync_FindUser_IsSuccessful()
        {
            var usr1 = new IdentityUser { UserName = "FindByNameAsync_FindUser_IsSuccessful" };
            IdentityUser usr2;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                usr2 = await userStore.FindByNameAsync(usr1.UserName).ConfigureAwait(false);
            }

            Assert.IsNotNull(usr2);
        }

        [Test]
        public async Task FindByNameAsync_DuplicateUser_Throws()
        {
            var usr1 = new IdentityUser { UserName = "FindByNameAsync_FindDuplicateUser_Throws" };
            var usr2 = new IdentityUser { UserName = "FindByNameAsync_FindDuplicateUser_Throws" };

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.StoreAsync(usr2).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<NonUniqueObjectException>(async () => await userStore.FindByNameAsync(usr1.UserName).ConfigureAwait(false));
            }

            Assert.IsNotNull(usr2);
        }

        [Test]
        public async Task SetPasswordHashAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPasswordHashAsync(null, "alkjdfejl").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPasswordHashAsyncSetPasswordHashIsSuccessfulTask()
        {
            var usr = new IdentityUser { UserName = "SetPasswordHashAsync_SetPasswordHash_IsSuccessful" };
            var passwordHash = "kjadlfjlkasdfjoe";

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetPasswordHashAsync(usr, passwordHash);
            }

            Assert.AreEqual(passwordHash, usr.PasswordHash);
        }

        [Test]
        public async Task GetPasswordHashAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetPasswordHashAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetPasswordHashAsync_GetPasswordHash_ReturnsPassword()
        {
            var passwordHash = "kjadlfjlkasdfjoe";
            var usr = new IdentityUser { UserName = "GetPasswordHashAsync_GetPasswordHash_ReturnsPassword", PasswordHash = passwordHash};
            
            using (var userStore = GetNewUserStore())
            {
                await userStore.GetPasswordHashAsync(usr);
            }

            Assert.AreEqual(passwordHash, usr.PasswordHash);
        }

        [Test]
        public async Task HasPasswordAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.HasPasswordAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task HasPasswordAsync_HasPassword_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "HasPasswordAsync_HasPassword_ReturnsTrue", PasswordHash = "kjadlfjlkasdfjoe" };
            bool result;

            using (var userStore = GetNewUserStore())
            {
                result = await userStore.HasPasswordAsync(usr);
            }

            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasPasswordAsync_HasNoPasswordHash_ReturnsFalse()
        {
            var usr = new IdentityUser { UserName = "GetPasswordHashAsync_GetPasswordHash_ReturnsPassword" };
            bool result;

            using (var userStore = GetNewUserStore())
            {
                result = await userStore.HasPasswordAsync(usr);
            }

            Assert.IsFalse(result);
        }

        [Test]
        public async Task AddLoginAsync_UserParameterNull_Throws()
        {
            var login = new UserLoginInfo("loginProvider", "providerKey");
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddLoginAsync(null, login).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddLoginAsync_LoginParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "AddLoginAsync_LoginParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddLoginAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddLoginAsync_AddLogin_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "AddLoginAsync_AddLogin_IsSuccessful" };
            var login = new UserLoginInfo("loginProvider", "providerKey");

            using (var userStore = GetNewUserStore())
            {
                await userStore.AddLoginAsync(usr, login).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.Logins.Contains(login));
        }

        [Test]
        public async Task AddLoginAsync_AddSameLoginTwiceOnlyAddedOnceToList_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "AddLoginAsync_AddSameLoginTwiceOnlyAddedOnceToList_IsSuccessful" };
            var login = new UserLoginInfo("loginProvide", "providerKey");

            using (var userStore = GetNewUserStore())
            {
                await userStore.AddLoginAsync(usr, login).ConfigureAwait(false);
                await userStore.AddLoginAsync(usr, login).ConfigureAwait(false);
            }

            Assert.AreEqual(1, usr.Logins.Count(x => x == login));
        }

        [Test]
        public async Task RemoveLoginAsync_UserParameterNull_Throws()
        {
            var login = new UserLoginInfo("loginProvider", "providerKey");
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveLoginAsync(null, login).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveLoginAsync_LoginParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "RemoveLoginAsync_LoginParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveLoginAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveLoginAsync_RemoveLogin_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "RemoveLoginAsync_RemoveLogin_IsSuccessful" };
            var login = new UserLoginInfo("loginProvider", "providerKey");
            usr.Logins.Add(login);

            using (var userStore = GetNewUserStore())
            {
                await userStore.RemoveLoginAsync(usr, login).ConfigureAwait(false);
            }

            Assert.AreEqual(0, usr.Logins.Count);
        }

        [Test]
        public async Task GetLoginsAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetLoginsAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetLoginsAsync_GetLogins_ReturnsLogins()
        {
            var usr = new IdentityUser { UserName = "GetLoginsAsync_GetLogins_IsSuccessful" };
            var login1 = new UserLoginInfo("loginProvider1", "providerKey1");
            var login2 = new UserLoginInfo("loginProvider2", "providerKey2");
            usr.Logins.Add(login1);
            usr.Logins.Add(login2);
            IList<UserLoginInfo> loginResult;

            using (var userStore = GetNewUserStore())
            {
                loginResult = await userStore.GetLoginsAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(2, loginResult.Count);
            Assert.IsTrue(loginResult.Contains(login1));
            Assert.IsTrue(loginResult.Contains(login2));
        }

        [Test]
        public async Task FindAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.FindAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FindAsync_FindLogin_ReturnUser()
        {
            var usr1 = new IdentityUser { UserName = "FindAsync_FindLogin_ReturnUser" };
            var login = new UserLoginInfo("loginProvider", "providerKey");
            usr1.Logins.Add(login);
            IdentityUser usr2;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                usr2 = await userStore.FindAsync(login).ConfigureAwait(false);
            }

            Assert.AreEqual(usr1.Id, usr2.Id);
        }

        [Test]
        public async Task FindAsync_DuplicateLogin_Throws()
        {
            var usr1 = new IdentityUser { UserName = "FindAsync_DuplicateLogin_Throws1" };
            var usr2 = new IdentityUser { UserName = "FindAsync_DuplicateLogin_Throws2" };
            var login = new UserLoginInfo("loginProvider", "providerKey");
            usr1.Logins.Add(login);
            usr2.Logins.Add(login);

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.StoreAsync(usr2).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<NonUniqueObjectException>(async () => await userStore.FindAsync(login).ConfigureAwait(false));
            }
        }

        [Test]
        public async Task GetClaimsAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetClaimsAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetClaimsAsync_GetClaims_ReturnsClaims()
        {
            var usr = new IdentityUser { UserName = "GetClaimsAsync_GetClaims_ReturnsClaims" };
            var claim1 = new Claim("ClaimType1", "ClaimValue1");
            var claim2 = new Claim("ClaimType2", "ClaimValue2");
            usr.Claims.Add(claim1);
            usr.Claims.Add(claim2);
            IList<Claim> claimResult;

            using (var userStore = GetNewUserStore())
            {
                claimResult = await userStore.GetClaimsAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(2, claimResult.Count);
            Assert.IsTrue(claimResult.Any(x => x.Type == claim1.Type && x.Value == claim1.Value));
            Assert.IsTrue(claimResult.Any(x => x.Type == claim2.Type && x.Value == claim2.Value));
        }

        [Test]
        public async Task AddClaimAsync_UserParameterNull_Throws()
        {
            var claim = new Claim("ClaimType", "ClaimValue");
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddClaimAsync(null, claim).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddClaimAsync_ClaimParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "AddClaimAsync_ClaimParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddClaimAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddClaimAsync_AddClaim_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "AddClaimAsync_AddClaim_IsSuccessful" };
            var claim = new Claim("ClaimType", "ClaimValue");

            using (var userStore = GetNewUserStore())
            {
                await userStore.AddClaimAsync(usr, claim).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.Claims.Any(x => x.Type == claim.Type && x.Value == claim.Value));
        }

        [Test]
        public async Task RemoveClaimAsync_UserParameterNull_Throws()
        {
            var claim = new Claim("ClaimType", "ClaimValue");
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveClaimAsync(null, claim).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveClaimAsync_ClaimParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "RemoveClaimAsync_ClaimParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveClaimAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveClaimAsync_RemoveClaim_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "RemoveLoginAsync_RemoveLogin_IsSuccessful" };
            var claim = new Claim("ClaimType", "ClaimValue");
            usr.Claims.Add(claim);

            using (var userStore = GetNewUserStore())
            {
                await userStore.RemoveClaimAsync(usr, new Claim(claim.Type, claim.Value)).ConfigureAwait(false);
            }

            Assert.AreEqual(0, usr.Logins.Count);
        }

        [Test]
        public async Task AddToRoleAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddToRoleAsync(null, "TestRole").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddToRoleAsync_RoleParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "AddToRoleAsync_RoleParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddToRoleAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddToRoleAsync_RoleParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "AddToRoleAsync_RoleParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddToRoleAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddToRoleAsync_RoleParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "AddToRoleAsync_RoleParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.AddToRoleAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task AddToRoleAsync_AddRole_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "AddToRoleAsync_AddRole_IsSuccessful" };
            const string role = "TestRole";

            using (var userStore = GetNewUserStore())
            {
                await userStore.AddToRoleAsync(usr, role).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.Roles.Contains(role));
        }

        [Test]
        public async Task RemoveFromRoleAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveFromRoleAsync(null, "TestRole").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveFromRole_RoleParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "RemoveFromRole_RoleParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveFromRoleAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveFromRoleAsync_RoleParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "RemoveFromRoleAsync_RoleParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveFromRoleAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveFromRoleAsync_RoleParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "RemoveFromRoleAsync_RoleParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.RemoveFromRoleAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RemoveFromRoleAsync_RemoveRole_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "RemoveFromRoleAsync_RemoveRole_IsSuccessful" };
            const string role = "TestRole";

            using (var userStore = GetNewUserStore())
            {
                await userStore.RemoveFromRoleAsync(usr, role).ConfigureAwait(false);
            }

            Assert.IsTrue(!usr.Roles.Contains(role));
        }

        [Test]
        public async Task GetRolesAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetRolesAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetRolesAsync_GetRoles_ReturnsRoles()
        {
            var usr = new IdentityUser { UserName = "GetRolesAsync_GetRoles_ReturnsRoles" };
            var role1 = "TestRole1";
            var role2 = "TestRole2";
            usr.Roles.Add(role1);
            usr.Roles.Add(role2);
            IList<string> roleResults;

            using (var userStore = GetNewUserStore())
            {
                roleResults = await userStore.GetRolesAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(2, roleResults.Count);
            Assert.IsTrue(roleResults.Contains(role1));
            Assert.IsTrue(roleResults.Contains(role2));
        }

        [Test]
        public async Task IsInRoleAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.IsInRoleAsync(null, "TestRole").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task IsInRoleAsync_RoleParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "IsInRoleAsync_RoleParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.IsInRoleAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task IsInRoleAsync_RoleParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "IsInRoleAsync_RoleParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.IsInRoleAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task IsInRoleAsync_RoleParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "IsInRoleAsync_RoleParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.IsInRoleAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task IsInRoleAsync_IsInRole_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "IsInRoleAsync_IsInRole_ReturnsTrue" };
            const string role = "TestRole";
            usr.Roles.Add(role);
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.IsInRoleAsync(usr, role).ConfigureAwait(false);
            }

            Assert.IsTrue(res);
        }

        [Test]
        public async Task IsInRoleAsync_IsNotInRole_ReturnsFalse()
        {
            var usr = new IdentityUser { UserName = "IsInRoleAsync_IsNotInRole_ReturnsFalse" };
            usr.Roles.Add("TestRole");
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.IsInRoleAsync(usr, "TestRole2").ConfigureAwait(false);
            }

            Assert.IsFalse(res);
        }

        [Test]
        public async Task SetSecurityStampAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetSecurityStampAsync(null, "SecurityStamp").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetSecurityStampAsync_SecurityStampParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "SetSecurityStampAsync_SecurityStampParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetSecurityStampAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetSecurityStampAsync_SecurityStampParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetSecurityStampAsync_RoleParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetSecurityStampAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetSecurityStampAsync_SecurityStampParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetSecurityStampAsync_RoleParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetSecurityStampAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetSecurityStampAsync_SetSecurityStamp_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetSecurityStampAsync_SetSecurityStamp_IsSuccessful" };
            const string securityStamp = "SecurityStamp";

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetSecurityStampAsync(usr, securityStamp).ConfigureAwait(false);
            }

            Assert.AreEqual(securityStamp, usr.SecurityStamp);
        }

        [Test]
        public async Task GetSecurityStampAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetSecurityStampAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetSecurityStampAsync_GetSecurityStamp_ReturnsSecurityStamp()
        {
            var usr = new IdentityUser { UserName = "GetSecurityStampAsync_GetSecurityStamp_ReturnsSecurityStamp" };
            const string securityStamp = "SecurityStamp";
            usr.SecurityStamp = securityStamp;
            string res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetSecurityStampAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(securityStamp, res);
        }

        [Test]
        public async Task SetEmailAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetEmailAsync(null, "test@admaos.ch").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetEmailAsync_EmailParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "SetEmailAsync_EmailParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetEmailAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetEmailAsync_EmailParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetEmailAsync_EmailParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetEmailAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetEmailAsync_EmailParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetEmailAsync_EmailParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetEmailAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetEmailAsync_SetEmail_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetEmailAsync_SetEmail_IsSuccessful" };
            const string email = "test@admaos.ch";

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetEmailAsync(usr, email).ConfigureAwait(false);
            }

            Assert.AreEqual(email, usr.Email);
        }

        [Test]
        public async Task GetEmailAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetEmailAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetEmailAsync_GetEmail_ReturnsEmail()
        {
            var usr = new IdentityUser { UserName = "GetEmailAsync_GetEmail_ReturnsEmail" };
            const string email = "test@admaos.ch";
            usr.Email = email;
            string res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetEmailAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(email, res);
        }

        [Test]
        public async Task GetEmailConfirmedAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetEmailConfirmedAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetEmailConfirmedAsync_GetEmailConfirmed_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "GetEmailConfirmedAsync_GetEmailConfirmed_ReturnsTrue", EmailConfirmed = true };
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetEmailConfirmedAsync(usr).ConfigureAwait(false);
            }

            Assert.IsTrue(res);
        }

        [Test]
        public async Task SetEmailConfirmedAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetEmailConfirmedAsync(null, false).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetEmailConfirmedAsync_SetEmailConfirmed_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetEmailConfirmedAsync_SetEmailConfirmed_IsSuccessful" };

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetEmailConfirmedAsync(usr, true).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.EmailConfirmed);
        }

        [Test]
        public async Task FindByEmailAsync_EmailParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.FindByEmailAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FindByEmailAsync_EmailParameterEmptyString_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.FindByEmailAsync("").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FindByEmailAsync_EmailParameterWhiteSpaceString_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.FindByEmailAsync("   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FindByEmailAsync_FindUserByEmail_ReturnsUser()
        {
            var usr1 = new IdentityUser { UserName = "FindByEmailAsync_FindUserByEmail_ReturnsUser1", Email = "test1@admaos.ch"};
            var usr2 = new IdentityUser { UserName = "FindByEmailAsync_FindUserByEmail_ReturnsUser2", Email = "test2@admaos.ch" };
            IdentityUser res1;
            IdentityUser res2;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.StoreAsync(usr2).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                res1 = await userStore.FindByEmailAsync(usr1.Email).ConfigureAwait(false);
                res2 = await userStore.FindByEmailAsync(usr2.Email).ConfigureAwait(false);
            }

            Assert.AreEqual(usr1.Id, res1.Id);
            Assert.AreEqual(usr2.Id, res2.Id);
        }

        [Test]
        public async Task FindByEmailAsync_DuplicateEmail_Throws()
        {
            var usr1 = new IdentityUser { UserName = "FindByEmailAsync_FindUserByEmail_ReturnsUser1", Email = "test1@admaos.ch" };
            var usr2 = new IdentityUser { UserName = "FindByEmailAsync_FindUserByEmail_ReturnsUser2", Email = "test1@admaos.ch" };

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.StoreAsync(usr2).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<NonUniqueObjectException>(async () => await userStore.FindByEmailAsync(usr1.Email).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetLockoutEndDateAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetLockoutEndDateAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetLockoutEndDateAsync_GetLockoutEndDate_ReturnsLockoutEndDate()
        {
            var usr = new IdentityUser { UserName = "GetLockoutEndDateAsync_GetLockoutEndDate_ReturnsLockoutEndDate" };
            var lockoutEndDate = new DateTimeOffset(new DateTime(2016, 3, 4, 12, 41, 0, DateTimeKind.Utc));
            usr.LockoutEndDateUtc = lockoutEndDate.DateTime;
            DateTimeOffset res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetLockoutEndDateAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(lockoutEndDate, res);
        }

        [Test]
        public async Task SetLockoutEndDateAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetLockoutEndDateAsync(null, new DateTimeOffset(new DateTime(2016, 3, 24, 13, 24, 00, DateTimeKind.Utc))).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetLockoutEndDateAsync_SetLockoutEnd_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetLockoutEndDateAsync_SetLockoutEnd_IsSuccessful" };
            var lockoutEnd = new DateTimeOffset(new DateTime(2016, 3, 24, 13, 24, 00, DateTimeKind.Utc));

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetLockoutEndDateAsync(usr, lockoutEnd).ConfigureAwait(false);
            }

            Assert.AreEqual(lockoutEnd.DateTime, usr.LockoutEndDateUtc);
        }

        [Test]
        public async Task IncrementAccessFailedCountAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.IncrementAccessFailedCountAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task IncrementAccessFailedCountAsync_IncrementAccessFailedCount_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "IncrementAccessFailedCountAsync_IncrementAccessFailedCount_IsSuccessful" };
            int res;

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
                using (var userStore = GetNewUserStore(sess))
                {
                    await userStore.IncrementAccessFailedCountAsync(usr).ConfigureAwait(false);
                    await userStore.IncrementAccessFailedCountAsync(usr).ConfigureAwait(false);
                    res = await userStore.IncrementAccessFailedCountAsync(usr).ConfigureAwait(false);
                }
            }
            
            Assert.AreEqual(3, res);
        }

        [Test]
        public async Task ResetAccessFailedCountAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.ResetAccessFailedCountAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task ResetAccessFailedCountAsync_ResetAccessFailedCount_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "ResetAccessFailedCountAsync_ResetAccessFailedCount_IsSuccessful", AccessFailedCount = 3 };

            using (var userStore = GetNewUserStore())
            {
                await userStore.ResetAccessFailedCountAsync(usr).ConfigureAwait(false);
            }
            
            Assert.AreEqual(0, usr.AccessFailedCount);
        }

        [Test]
        public async Task GetAccessFailedCountAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetAccessFailedCountAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetAccessFailedCountAsync_GetAccessFailedCount_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "GetAccessFailedCountAsync_GetAccessFailedCount_IsSuccessful", AccessFailedCount = 3 };
            int res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetAccessFailedCountAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(3, res);
        }

        [Test]
        public async Task GetLockoutEnabledAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetLockoutEnabledAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetLockoutEnabledAsync_GetLockoutEnabled_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "GetLockoutEnabledAsync_GetLockoutEnabled_ReturnsTrue", LockoutEnabled = true };
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetLockoutEnabledAsync(usr).ConfigureAwait(false);
            }

            Assert.IsTrue(res);
        }

        [Test]
        public async Task SetLockoutEnabledAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetLockoutEnabledAsync(null, true).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetLockoutEnabledAsync_SetLockoutEnabled_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetLockoutEnabledAsync_SetLockoutEnabled_IsSuccessful" };

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetLockoutEnabledAsync(usr, true).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.LockoutEnabled);
        }

        [Test]
        public async Task SetTwoFactorEnabledAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetTwoFactorEnabledAsync(null, true).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetTwoFactorEnabledAsync_SetTwoFactorEnabled_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetTwoFactorEnabledAsync_SetTwoFactorEnabled_IsSuccessful" };

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetTwoFactorEnabledAsync(usr, true).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.TwoFactorEnabled);
        }

        [Test]
        public async Task GetTwoFactorEnabledAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetTwoFactorEnabledAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetTwoFactorEnabledAsync_GetTwoFactorEnabled_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "GetTwoFactorEnabledAsync_GetTwoFactorEnabled_ReturnsTrue", TwoFactorEnabled = true };
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetTwoFactorEnabledAsync(usr).ConfigureAwait(false);
            }

            Assert.IsTrue(res);
        }

        [Test]
        public async Task SetPhoneNumberAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPhoneNumberAsync(null, "+41 41 123 45 67").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPhoneNumberAsync_PhoneNumberParameterNull_Throws()
        {
            var usr = new IdentityUser { UserName = "SetPhoneNumberAsync_PhoneNumberParameterNull_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPhoneNumberAsync(usr, null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPhoneNumberAsync_PhoneNumberParameterEmptyString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetPhoneNumberAsync_PhoneNumberParameterEmptyString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPhoneNumberAsync(usr, "").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPhoneNumberAsync_PhoneNumberParameterWhiteSpaceString_Throws()
        {
            var usr = new IdentityUser { UserName = "SetPhoneNumberAsync_PhoneNumberParameterWhiteSpaceString_Throws" };
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPhoneNumberAsync(usr, "   ").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPhoneNumberAsync_SetPhoneNumber_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetPhoneNumberAsync_SetPhoneNumber_IsSuccessful" };
            const string phoneNumber = "+41 41 123 45 67";

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetPhoneNumberAsync(usr, phoneNumber).ConfigureAwait(false);
            }

            Assert.AreEqual(phoneNumber, usr.PhoneNumber);
        }

        [Test]
        public async Task GetPhoneNumberAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetPhoneNumberAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetPhoneNumberAsync_GetTwoFactorEnabled_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "GetPhoneNumberAsync_GetTwoFactorEnabled_ReturnsTrue", PhoneNumber = "+41 41 123 45 67" };
            string res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetPhoneNumberAsync(usr).ConfigureAwait(false);
            }

            Assert.AreEqual(usr.PhoneNumber, res);
        }

        [Test]
        public async Task GetPhoneNumberConfirmedAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.GetPhoneNumberConfirmedAsync(null).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetPhoneNumberConfirmedAsync_GetPhoneNumberConfirmed_ReturnsTrue()
        {
            var usr = new IdentityUser { UserName = "GetPhoneNumberConfirmedAsync_GetPhoneNumberConfirmed_ReturnsTrue", PhoneNumberConfirmed = true};
            bool res;

            using (var userStore = GetNewUserStore())
            {
                res = await userStore.GetPhoneNumberConfirmedAsync(usr).ConfigureAwait(false);
            }

            Assert.IsTrue(res);
        }

        [Test]
        public async Task SetPhoneNumberConfirmedAsync_UserParameterNull_Throws()
        {
            using (var userStore = GetNewUserStore())
            {
                await ThrowsAsync<ArgumentNullException>(async () => await userStore.SetPhoneNumberConfirmedAsync(null, true ).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SetPhoneNumberConfirmedAsync_SetPhoneNumberConfirmed_IsSuccessful()
        {
            var usr = new IdentityUser { UserName = "SetPhoneNumberConfirmedAsync_SetPhoneNumberConfirmed_IsSuccessful" };

            using (var userStore = GetNewUserStore())
            {
                await userStore.SetPhoneNumberConfirmedAsync(usr, true).ConfigureAwait(false);
            }

            Assert.IsTrue(usr.PhoneNumberConfirmed);
        }

        [Test]
        public async Task Users_GetAllUsers_ListOfUsers()
        {
            var usr1 = new IdentityUser { UserName = "Users_GetAllUsers_ListOfUsers_1" };
            var usr2 = new IdentityUser { UserName = "Users_GetAllUsers_ListOfUsers_2" };
            var usr3 = new IdentityUser { UserName = "Users_GetAllUsers_ListOfUsers_3" };
            var usr4 = new IdentityUser { UserName = "Users_GetAllUsers_ListOfUsers_4" };

            using (var sess = _store.OpenAsyncSession())
            {
                await sess.StoreAsync(usr1).ConfigureAwait(false);
                await sess.StoreAsync(usr2).ConfigureAwait(false);
                await sess.StoreAsync(usr3).ConfigureAwait(false);
                await sess.StoreAsync(usr4).ConfigureAwait(false);
                await sess.SaveChangesAsync().ConfigureAwait(false);
            }

            IList<IdentityUser> users;
            using (var userStore = GetNewUserStore())
            {
                users = await userStore.Users.ToListAsync().ConfigureAwait(false);
            }

            Assert.AreEqual(users.Count, 4);
        }
    }
}

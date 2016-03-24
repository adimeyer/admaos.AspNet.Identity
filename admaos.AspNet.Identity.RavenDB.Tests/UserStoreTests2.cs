/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using NUnit.Framework;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.UniqueConstraints;
using Raven.Tests.Helpers;

// http://ravendb.net/docs/article-page/2.5/csharp/samples/raven-tests/createraventests
// http://ravendb.net/docs/article-page/3.0/csharp/server/troubleshooting/sending-support-ticket#writing-unit-tests

namespace admaos.AspNet.Identity.RavenDB.Tests
{
    
    //public class UserStoreTests2 : RavenTestBase
    //{
    //    public UserStoreTests2()
    //    {
    //        this.CreateServer(8077);
    //    }
    //}

    [TestFixture]
    public class UserStoreTests2 : RavenTestBase
    {
        private IAsyncDocumentSession _session;

        public UserManager<IdentityUser> GetNewUserManager()
        {
            return new UserManager<IdentityUser>(new UserStore<IdentityUser>(_session));
        }

        [SetUp]
        public void SetUp()
        {
            var store = new EmbeddableDocumentStore
            {
                Configuration =
                {
                    RunInMemory = true,
                    RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
                    Storage =
                    {
                        Voron =
                        {
                            AllowOn32Bits = true
                        }
                    }
                }
            };

            //var store = new DocumentStore()
            //{
            //    Url = "http://localhost:8080"
            //};
            store.RegisterListener(new UniqueConstraintsStoreListener());
            store.Initialize();
            //store.DatabaseCommands.GlobalAdmin.DeleteDatabase("Test", hardDelete: true);
            //store.DatabaseCommands.GlobalAdmin.CreateDatabase(new DatabaseDocument
            //{
            //    Id = "Test",
            //    Settings = {
            //            { "Raven/DataDir", "~/Test" }
            //        }
            //});

            _session = store.OpenAsyncSession();
        }

        [TearDown]
        public void TearDown()
        {
            _session.Dispose();
        }

        [Test]
        public void UserStoreConstructor_UniqueConstraintStoreListenerNotRegistered_ThrowsException()
        {
            var store = new DocumentStore()
            {
                Url = "http://localhost:8080"
            };
            store.Initialize();
            store.DatabaseCommands.GlobalAdmin.DeleteDatabase("Test", hardDelete: true);
            store.DatabaseCommands.GlobalAdmin.CreateDatabase(new DatabaseDocument
            {
                Id = "Test",
                Settings =
                {
                    {"Raven/DataDir", "~/Test"}
                }
            });
            var session = store.OpenAsyncSession("Test");
            Assert.Throws(typeof(InvalidOperationException),
                delegate { new UserManager<IdentityUser>(new UserStore<IdentityUser>(session)); });
        }

        [Test]
        public void UserStoreConstructor_UniqueConstraintBundleMissing_ThrowsException()
        {
            throw new NotImplementedException();
        }

        [Test]
        public async Task CreateAsync_CheckIfSavedToRavenDB_IsSaved()
        {
            const string userName = "CreateAsync_CheckIfSavedToRavenDB_IsSaved@admaos.ch";
            const string password = "1234test";

            var createdUser = new IdentityUser { UserName = userName };

            using (var mgr = GetNewUserManager())
            {
                var createUserResult = await mgr.CreateAsync(createdUser, password).ConfigureAwait(false);
                var loadedUser = await _session.LoadAsync<IdentityUser>(createdUser.Id).ConfigureAwait(false);

                Assert.IsTrue(createUserResult.Succeeded);
                Assert.AreSame(createdUser, loadedUser);
            }
        }

        [Test]
        public async Task CreateAsync_DuplicateUserName_Fails()
        {
            const string userName = "CreateAsync_DuplicateUserName_Fails@admaos.ch";
            const string password = "1234test";

            var createdUser1 = new IdentityUser { UserName = userName };
            var createdUser2 = new IdentityUser { UserName = userName };

            using (var mgr = GetNewUserManager())
            {
                var createUserResult1 = await mgr.CreateAsync(createdUser1, password).ConfigureAwait(false);
                var createUserResult2 = await mgr.CreateAsync(createdUser2, password).ConfigureAwait(false);

                Assert.IsTrue(createUserResult1.Succeeded);
                Assert.IsFalse(createUserResult2.Succeeded);
                Assert.AreEqual(1, createUserResult2.Errors.Count());
                Assert.Contains("Name " + userName + " is already taken.", createUserResult2.Errors.ToList());
            }
        }

        [Test]
        public async Task UpdateAsync_UpdateUserName_Succeeds()
        {
            const string userName = "UpdateAsync_UpdateUserName_Succeeds@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                createdUser.UserName = "UpdatedUserName@admaos.ch";
                var updateResult2 = await mgr.UpdateAsync(createdUser).ConfigureAwait(false);

                Assert.IsTrue(updateResult2.Succeeded);
            }
        }

        [Test]
        public async Task DeleteAsync_DeleteUserFromRavenDB_IsDeleted()
        {
            const string userName = "DeleteAsync_DeleteUserFromRavenDB_IsDeleted@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var deleteResult = await mgr.DeleteAsync(createdUser).ConfigureAwait(false);
                var findUser = await _session.LoadAsync<IdentityUser>(createdUser.Id).ConfigureAwait(false);

                Assert.IsTrue(deleteResult.Succeeded);
                Assert.IsNull(findUser);
            }
        }

        [Test]
        public async Task FindByIdAsync_FindUser_IsSuccessful()
        {
            const string userName = "FindByIdAsync_FindUser_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var loadedUser = await mgr.FindByIdAsync(createdUser.Id).ConfigureAwait(false);

                Assert.IsNotNull(loadedUser);
                Assert.AreSame(createdUser, loadedUser);
            }
        }

        [Test]
        public async Task FindByNameAsync_FindUser_IsSuccessful()
        {
            const string userName = "FindByNameAsync_FindUser_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var loadedUser = await mgr.FindByNameAsync(userName).ConfigureAwait(false);

                Assert.IsNotNull(loadedUser);
                Assert.AreSame(createdUser, loadedUser);
            }
        }

        [Test]
        public async Task SetPasswordAsync_SetNewPassword_IsSuccessful()
        {
            const string userName = "SetPasswordAsync_SetNewPassword_IsSuccessful@admaos.ch";
            const string initialPassword = "1234test";
            const string initialPasswordHash = "AJp84hsO1ozKZlYSZzh+EWIPYWqO+0c5HY2boyiY/OL6dPm9zbXSj2rNrEBvhcYsxA==";
            const string newPassword = "newPassword1234";
            var createdUser = new IdentityUser {UserName = userName, PasswordHash = initialPasswordHash};

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var passwordChangeResult = await mgr.ChangePasswordAsync(createdUser.Id, initialPassword, newPassword).ConfigureAwait(false);

                Assert.IsTrue(passwordChangeResult.Succeeded);
                Assert.AreNotEqual(initialPasswordHash, createdUser.PasswordHash);
            }
        }

        [Test]
        public async Task HasPasswordAsync_HasPassword_IsTrue()
        {
            const string userName = "HasPasswordAsync_HasPassword_IsTrue@admaos.ch";
            const string newPassword = "newPassword1234";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var hasInitialPassword = await mgr.HasPasswordAsync(createdUser.Id).ConfigureAwait(false);
                var setPasswordResult = await mgr.AddPasswordAsync(createdUser.Id, newPassword).ConfigureAwait(false);
                var hasNewPassword = await mgr.HasPasswordAsync(createdUser.Id).ConfigureAwait(false);

                Assert.IsFalse(hasInitialPassword);
                Assert.IsTrue(setPasswordResult.Succeeded);
                Assert.IsTrue(hasNewPassword);
            }
        }

        [Test]
        public async Task AddLoginAsync_AddNewLogin_IsSuccessful()
        {
            const string userName = "AddLoginAsync_AddNewLogin_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userLoginInfo = new UserLoginInfo("Google", "http://www.google.com/fake/user/identifier");

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var addLoginResult = await mgr.AddLoginAsync(createdUser.Id, userLoginInfo).ConfigureAwait(false);
                var loginCount = createdUser.Logins.Count;

                Assert.IsTrue(addLoginResult.Succeeded);
                Assert.AreEqual(1, loginCount);
                Assert.IsTrue(createdUser.Logins.Any(x => x.LoginProvider == "Google"));
            }
        }

        [Test]
        public async Task AddLoginAsync_AddDuplicateLogin_Fails()
        {
            const string userName = "AddLoginAsync_AddDuplicateLogin_Fails@admaos.ch";
            var createdUser1 = new IdentityUser { UserName = userName };
            var createdUser2 = new IdentityUser { UserName = "2" };
            var userLoginInfo = new UserLoginInfo("Google", "http://www.google.com/fake/user/identifier");

            await _session.StoreAsync(createdUser1).ConfigureAwait(false);
            await _session.StoreAsync(createdUser2).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var addLoginResult1 = await mgr.AddLoginAsync(createdUser1.Id, userLoginInfo).ConfigureAwait(false);
                var addLoginResult2 = await mgr.AddLoginAsync(createdUser2.Id, userLoginInfo).ConfigureAwait(false);

                Assert.IsTrue(addLoginResult1.Succeeded);
                Assert.IsFalse(addLoginResult2.Succeeded);
            }
        }

        [Test]
        public async Task RemoveLoginAsync_RemoveLogin_IsSuccessful()
        {
            const string userName = "RemoveLoginAsync_RemoveLogin_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userLogin = new UserLoginInfo("Google", "http://www.google.com/fake/user/identifier");
            createdUser.Logins.Add(userLogin);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialLoginCount = createdUser.Logins.Count;
                var removeLoginResult = await mgr.RemoveLoginAsync(createdUser.Id, userLogin).ConfigureAwait(false);
                var newLoginCount = createdUser.Logins.Count;

                Assert.AreEqual(1, initialLoginCount);
                Assert.IsTrue(removeLoginResult.Succeeded);
                Assert.AreEqual(0, newLoginCount);
            }
        }

        [Test]
        public async Task GetLoginsAsync_CountLogins_IsSuccessful()
        {
            const string userName = "GetLoginsAsync_CountLogins_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userLogin1 = new UserLoginInfo("Google", "http://www.google.com/fake/user/identifier");
            var userLogin2 = new UserLoginInfo("Yahoo", "http://www.yahoo.com/fake/user/identifier");
            createdUser.Logins.Add(userLogin1);
            createdUser.Logins.Add(userLogin2);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialLoginCount = createdUser.Logins.Count;
                var returnedLogins = await mgr.GetLoginsAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(2, initialLoginCount);
                Assert.AreEqual(2, returnedLogins.Count);
                Assert.AreEqual(1, returnedLogins.Count(x => x.LoginProvider == userLogin1.LoginProvider));
                Assert.AreEqual(1, returnedLogins.Count(x => x.LoginProvider == userLogin1.LoginProvider));
            }
        }

        [Test]
        public async Task FindAsync_FindAsync_IsSuccessful()
        {
            const string userName = "FindAsync_FindAsync_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userLogin = new UserLoginInfo("Google", "http://www.google.com/fake/user/identifier");
            createdUser.Logins.Add(userLogin);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var foundUser = await mgr.FindAsync(userLogin).ConfigureAwait(false);

                Assert.IsNotNull(foundUser);
                Assert.AreSame(createdUser, foundUser);
            }
        }

        [Test]
        public async Task GetClaimsAsync_CountClaims_IsSuccessful()
        {
            const string userName = "GetClaimsAsync_CheckClaims_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userClaim1 = new IdentityUserClaim { Type = "TestType1", Value = "TestValue1"};
            var userClaim2 = new IdentityUserClaim { Type = "TestType2", Value = "TestValue2" };
            createdUser.Claims.Add(userClaim1);
            createdUser.Claims.Add(userClaim2);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialClaimsCount = createdUser.Claims.Count;
                var returnedClaims = await mgr.GetClaimsAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(2, initialClaimsCount);
                Assert.AreEqual(2, returnedClaims.Count);
                Assert.AreEqual(1, returnedClaims.Count(x => x.Type == userClaim1.Type && x.Value == userClaim1.Value));
                Assert.AreEqual(1, returnedClaims.Count(x => x.Type == userClaim2.Type && x.Value == userClaim2.Value));
            }
        }

        [Test]
        public async Task AddClaimsAsync_AddNewClaim_IsSuccessful()
        {
            const string userName = "AddClaimsAsync_AddNewClaim_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userClaim1 = new Claim("TestType1", "TestValue1");

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialClaimsCount = createdUser.Claims.Count;
                var addClaimResult = await mgr.AddClaimAsync(createdUser.Id, userClaim1).ConfigureAwait(false);

                Assert.AreEqual(0, initialClaimsCount);
                Assert.IsTrue(addClaimResult.Succeeded);
                Assert.AreEqual(1, createdUser.Claims.Count);
                Assert.AreEqual(1, createdUser.Claims.Count(x => x.Type == userClaim1.Type && x.Value == userClaim1.Value));
            }
        }

        [Test]
        public async Task RemoveClaimsAsync_RemoveClaim_IsSuccessful()
        {
            const string userName = "RemoveClaimsAsync_RemoveClaim_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            var userClaim = new IdentityUserClaim { Type = "TestType1", Value = "TestValue1" };
            var userClaimToRemove = new Claim(userClaim.Type, userClaim.Value);
            createdUser.Claims.Add(userClaim);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialClaimsCount = createdUser.Claims.Count;
                var removeClaimResult = await mgr.RemoveClaimAsync(createdUser.Id, userClaimToRemove).ConfigureAwait(false);
                var newClaimsCount = createdUser.Claims.Count;

                Assert.AreEqual(1, initialClaimsCount);
                Assert.IsTrue(removeClaimResult.Succeeded);
                Assert.AreEqual(0, newClaimsCount);
            }
        }

        [Test]
        public async Task AddToRoleAsync_AddToNewRole_IsSuccessful()
        {
            const string userName = "AddToRoleAsync_AddToNewRole_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            const string role = "TestRole";

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialRoleCount = createdUser.Roles.Count;
                var addToRoleResult = await mgr.AddToRoleAsync(createdUser.Id, role).ConfigureAwait(false);
                var newRoleCount = createdUser.Roles.Count;

                Assert.AreEqual(0, initialRoleCount);
                Assert.IsTrue(addToRoleResult.Succeeded);
                Assert.AreEqual(1, newRoleCount);
                Assert.AreEqual(1, createdUser.Roles.Count(x => x == role));
            }
        }

        [Test]
        public async Task RemoveFromRoleAsync_RemoveFromRole_IsSuccessful()
        {
            const string userName = "RemoveFromRoleAsync_RemoveFromRole_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            const string role = "TestRole";
            createdUser.Roles.Add(role);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialRoleCount = createdUser.Roles.Count;
                var addToRoleResult = await mgr.RemoveFromRoleAsync(createdUser.Id, role).ConfigureAwait(false);
                var newRoleCount = createdUser.Roles.Count;

                Assert.AreEqual(1, initialRoleCount);
                Assert.IsTrue(addToRoleResult.Succeeded);
                Assert.AreEqual(0, newRoleCount);
            }
        }

        [Test]
        public async Task GetRolesAsync_GetRoles_IsSuccessful()
        {
            const string userName = "GetRolesAsync_GetRoles_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            const string role1 = "TestRole1";
            const string role2 = "TestRole2";
            createdUser.Roles.Add(role1);
            createdUser.Roles.Add(role2);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialRoleCount = createdUser.Roles.Count;
                var returnedRoles = await mgr.GetRolesAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(2, initialRoleCount);
                Assert.AreEqual(2, returnedRoles.Count);
                Assert.AreEqual(1, returnedRoles.Count(x => x == role1));
                Assert.AreEqual(1, returnedRoles.Count(x => x == role2));
            }
        }

        [Test]
        public async Task IsInRoleAsync_IsInRole_IsSuccessful()
        {
            const string userName = "IsInRoleAsync_IsInRole_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };
            const string role1 = "TestRole1";
            const string role2 = "TestRole2";
            createdUser.Roles.Add(role1);
            createdUser.Roles.Add(role2);

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var isInRole1 = await mgr.IsInRoleAsync(createdUser.Id, role1).ConfigureAwait(false);
                var isInRole2 = await mgr.IsInRoleAsync(createdUser.Id, role2).ConfigureAwait(false);
                var isInRole3 = await mgr.IsInRoleAsync(createdUser.Id, "TestRole").ConfigureAwait(false);

                Assert.IsTrue(isInRole1);
                Assert.IsTrue(isInRole2);
                Assert.IsFalse(isInRole3);
            }
        }

        [Test]
        public async Task SetSecurityStampAsync_SetSecurityStamp_IsSuccessful()
        {
            const string userName = "SetSecurityStampAsync_SetSecurityStamp_IsSuccessful@admaos.ch";
            const string securityStamp = "alkdsjfoijerlyadaf";
            var createdUser = new IdentityUser { UserName = userName, SecurityStamp = securityStamp };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var setSecurityStampResult = await mgr.UpdateSecurityStampAsync(createdUser.Id).ConfigureAwait(false);

                Assert.IsTrue(setSecurityStampResult.Succeeded);
                Assert.AreNotEqual(securityStamp, createdUser.SecurityStamp);
            }
        }

        [Test]
        public async Task GetSecurityStampAsync_GetSecurityStamp_IsSuccessful()
        {
            const string userName = "GetSecurityStampAsync_GetSecurityStamp_IsSuccessful@admaos.ch";
            const string securityStamp = "alkdsjfoijerlyadaf";
            var createdUser = new IdentityUser { UserName = userName, SecurityStamp = securityStamp };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedSecurityStamp = await mgr.GetSecurityStampAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(securityStamp, returnedSecurityStamp);
            }
        }

        [Test]
        public async Task SetEmailAsync_SetEmail_IsSuccessful()
        {
            const string userName = "SetEmailAsync_SetEmail_IsSuccessful@admaos.ch";
            const string email = "email@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var setEmailResult = await mgr.SetEmailAsync(createdUser.Id, email).ConfigureAwait(false);

                Assert.IsTrue(setEmailResult.Succeeded);
                Assert.AreEqual(email, createdUser.Email);
            }
        }

        [Test]
        public async Task GetEmailAsync_GetEmail_IsSuccessful()
        {
            const string userName = "GetEmailAsync_GetEmail_IsSuccessful@admaos.ch";
            const string email = "email@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName, Email = email };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedEmail = await mgr.GetEmailAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(email, returnedEmail);
            }
        }

        [Test]
        public async Task GetEmailConfirmedAsync_GetEmailConfirmed_IsSuccessful()
        {
            const string userNameConfirmed = "GetEmailConfirmedAsync_GetEmailConfirmed_IsSuccessful@admaos.ch";
            const string userNameNotConfirmed = "GetEmailConfirmedAsync_GetEmailConfirmed_IsSuccessful2@admaos.ch";
            var createdUserConfirmed = new IdentityUser { UserName = userNameConfirmed, EmailConfirmed = true };
            var createdUserNotConfirmed = new IdentityUser { UserName = userNameNotConfirmed, EmailConfirmed = false };

            await _session.StoreAsync(createdUserConfirmed).ConfigureAwait(false);
            await _session.StoreAsync(createdUserNotConfirmed).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedEmailConfirmed = await mgr.IsEmailConfirmedAsync(createdUserConfirmed.Id).ConfigureAwait(false);
                var returnedEmailNotConfirmed = await mgr.IsEmailConfirmedAsync(createdUserNotConfirmed.Id).ConfigureAwait(false);

                Assert.IsTrue(returnedEmailConfirmed);
                Assert.IsFalse(returnedEmailNotConfirmed);
            }
        }

        [Test]
        public async Task SetEmailConfirmedAsync_SetEmailConfirmed_IsSuccessful()
        {
            throw new NotImplementedException();
        }

        [Test]
        public async Task FindByEmailAsync_FindByEmail_IsSuccessful()
        {
            const string userName = "FindByEmailAsync_FindByEmail_IsSuccessful@admaos.ch";
            const string email = "test@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName, Email = email };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedUser = await mgr.FindByEmailAsync(email).ConfigureAwait(false);

                Assert.IsNotNull(returnedUser);
                Assert.AreEqual(email, returnedUser.Email);
                Assert.AreSame(createdUser, returnedUser);
            }
        }

        [Test]
        public async Task GetLockoutEndDateAsync_GetLockoutEndDate_IsSuccessful()
        {
            const string userName = "GetLockoutEndDateAsync_GetLockoutEndDate_IsSuccessful@admaos.ch";
            var lockoutEndDate = new DateTime(2016, 3, 12, 10, 9, 0, DateTimeKind.Utc);
            var createdUser = new IdentityUser { UserName = userName, LockoutEndDateUtc = lockoutEndDate };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedLockoutEndDate = await mgr.GetLockoutEndDateAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(lockoutEndDate, returnedLockoutEndDate.DateTime);
            }
        }

        [Test]
        public async Task SetLockoutEndDateAsync_SetLockoutEndDate_IsSuccessful()
        {
            const string userName = "SetLockoutEndDateAsync_SetLockoutEndDate_IsSuccessful@admaos.ch";
            var lockoutEndDate = new DateTime(2016, 3, 12, 10, 9, 0, DateTimeKind.Utc);
            var createdUser = new IdentityUser { UserName = userName, LockoutEnabled = true };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var setLockoutEndDateResult = await mgr.SetLockoutEndDateAsync(createdUser.Id, lockoutEndDate).ConfigureAwait(false);

                Assert.IsTrue(setLockoutEndDateResult.Succeeded);
                Assert.AreEqual(lockoutEndDate, createdUser.LockoutEndDateUtc);
            }
        }

        [Test]
        public async Task IncrementAccessFailedCountAsync_IncrementAccessFailedCount_IsSuccessful()
        {
            const string userName = "IncrementAccessFailedCountAsync_IncrementAccessFailedCount_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName, LockoutEnabled = true };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                mgr.MaxFailedAccessAttemptsBeforeLockout = 5;

                var accessFailedResults = new List<IdentityResult>();
                var accessFailedCounts = new List<int>();

                for (var i = 1; i <= mgr.MaxFailedAccessAttemptsBeforeLockout; i++)
                {
                    accessFailedResults.Add(await mgr.AccessFailedAsync(createdUser.Id).ConfigureAwait(false));
                    accessFailedCounts.Add(createdUser.AccessFailedCount);
                }

                for (var i = 1; i <= mgr.MaxFailedAccessAttemptsBeforeLockout; i++)
                {
                    Assert.IsTrue(accessFailedResults[i - 1].Succeeded);
                    Assert.AreEqual(i, accessFailedCounts[i-1]);
                }
            }
        }

        [Test]
        public async Task ResetAccessFailedCountAsync_ResetAccessFailedCount_IsSuccessful()
        {
            const string userName = "ResetAccessFailedCountAsync_ResetAccessFailedCount_IsSuccessful@admaos.ch";
            const int accessFailedCount = 3;
            var createdUser = new IdentityUser { UserName = userName, LockoutEnabled = true, AccessFailedCount = accessFailedCount };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var resetAccessFailedCountResult = await mgr.ResetAccessFailedCountAsync(createdUser.Id).ConfigureAwait(false);

                Assert.IsTrue(resetAccessFailedCountResult.Succeeded);
                Assert.AreEqual(0, createdUser.AccessFailedCount);
            }
        }

        [Test]
        public async Task GetAccessFailedCountAsync_GetAccessFailedCount_IsSuccessful()
        {
            const string userName = "GetAccessFailedCountAsync_GetAccessFailedCount_IsSuccessful@admaos.ch";
            const int accessFailedCount = 3;
            var createdUser = new IdentityUser { UserName = userName, LockoutEnabled = true, AccessFailedCount = accessFailedCount };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedGetAccessFailedCount = await mgr.GetAccessFailedCountAsync(createdUser.Id);

                Assert.AreEqual(accessFailedCount, returnedGetAccessFailedCount);
            }
        }

        [Test]
        public async Task GetLockoutEnabledAsync_GetLockoutEnabled_IsSuccessful()
        {
            const string userName1 = "GetLockoutEnabledAsync_GetLockoutEnabled_IsSuccessful@admaos.ch";
            const string userName2 = "GetLockoutEnabledAsync_GetLockoutEnabled_IsSuccessful2@admaos.ch";
            var createdUser1 = new IdentityUser { UserName = userName1, LockoutEnabled = true };
            var createdUser2 = new IdentityUser { UserName = userName2 };

            await _session.StoreAsync(createdUser1).ConfigureAwait(false);
            await _session.StoreAsync(createdUser2).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedLockoutEnabled1 = await mgr.GetLockoutEnabledAsync(createdUser1.Id);
                var returnedLockoutEnabled2 = await mgr.GetLockoutEnabledAsync(createdUser2.Id);

                Assert.IsTrue(returnedLockoutEnabled1);
                Assert.IsFalse(returnedLockoutEnabled2);
            }
        }

        [Test]
        public async Task SetLockoutEnabledAsync_SetLockoutEnabled_IsSuccessful()
        {
            const string userName = "GetLockoutEnabledAsync_GetLockoutEnabled_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialLockoutEnabled = createdUser.LockoutEnabled;
                var setLockoutEnabledResult = await mgr.SetLockoutEnabledAsync(createdUser.Id, true);

                Assert.IsFalse(initialLockoutEnabled);
                Assert.IsTrue(setLockoutEnabledResult.Succeeded);
                Assert.IsTrue(createdUser.LockoutEnabled);
            }
        }

        [Test]
        public async Task SetTwoFactorEnabledAsync_SetTwoFactorEnabled_IsSuccessful()
        {
            const string userName = "SetTwoFactorEnabledAsync_SetTwoFactorEnabled_IsSuccessful@admaos.ch";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var initialTwoFactorEnabled = createdUser.TwoFactorEnabled;
                var setTwoFactorEnabledResult = await mgr.SetTwoFactorEnabledAsync(createdUser.Id, true);

                Assert.IsFalse(initialTwoFactorEnabled);
                Assert.IsTrue(setTwoFactorEnabledResult.Succeeded);
                Assert.IsTrue(createdUser.TwoFactorEnabled);
            }
        }

        [Test]
        public async Task GetTwoFactorEnabledAsync_GetTwoFactorEnabled_IsSuccessful()
        {
            const string userName1 = "GetTwoFactorEnabledAsync_GetTwoFactorEnabled_IsSuccessful@admaos.ch";
            const string userName2 = "GetTwoFactorEnabledAsync_GetTwoFactorEnabled_IsSuccessful2@admaos.ch";
            var createdUser1 = new IdentityUser { UserName = userName1, TwoFactorEnabled = true };
            var createdUser2 = new IdentityUser { UserName = userName2 };

            await _session.StoreAsync(createdUser1).ConfigureAwait(false);
            await _session.StoreAsync(createdUser2).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedTwoFactorEnabled1 = await mgr.GetTwoFactorEnabledAsync(createdUser1.Id);
                var returnedTwoFactorEnabled2 = await mgr.GetTwoFactorEnabledAsync(createdUser2.Id);

                Assert.IsTrue(returnedTwoFactorEnabled1);
                Assert.IsFalse(returnedTwoFactorEnabled2);
            }
        }

        [Test]
        public async Task SetPhoneNumberAsync_SetPhoneNumber_IsSuccessful()
        {
            const string userName = "SetPhoneNumberAsync_SetPhoneNumber_IsSuccessful@admaos.ch";
            const string phoneNumber = "+41791234567";
            var createdUser = new IdentityUser { UserName = userName };

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var setPhoneNumberResult = await mgr.SetPhoneNumberAsync(createdUser.Id, phoneNumber).ConfigureAwait(false);

                Assert.IsTrue(setPhoneNumberResult.Succeeded);
                Assert.AreEqual(phoneNumber, createdUser.PhoneNumber);
            }
        }

        [Test]
        public async Task GetPhoneNumberAsync_GetPhoneNumber_IsSuccessful()
        {
            const string userName = "GetPhoneNumberAsync_GetPhoneNumber_IsSuccessful@admaos.ch";
            const string phoneNumber = "+41791234567";
            var createdUser = new IdentityUser { UserName = userName, PhoneNumber = phoneNumber};

            await _session.StoreAsync(createdUser).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedPhoneNumber = await mgr.GetPhoneNumberAsync(createdUser.Id).ConfigureAwait(false);

                Assert.AreEqual(phoneNumber, returnedPhoneNumber);
            }
        }

        [Test]
        public async Task GetPhoneNumberConfirmedAsync_GetPhoneNumberConfirmed_IsSuccessful()
        {
            const string userNameConfirmed = "GetPhoneNumberConfirmedAsync_GetPhoneNumberConfirmed_IsSuccessful@admaos.ch";
            const string userNameNotConfirmed = "GetPhoneNumberConfirmedAsync_GetPhoneNumberConfirmed_IsSuccessful2@admaos.ch";
            var createdUserConfirmed = new IdentityUser { UserName = userNameConfirmed, PhoneNumberConfirmed = true };
            var createdUserNotConfirmed = new IdentityUser { UserName = userNameNotConfirmed, PhoneNumberConfirmed = false };

            await _session.StoreAsync(createdUserConfirmed).ConfigureAwait(false);
            await _session.StoreAsync(createdUserNotConfirmed).ConfigureAwait(false);
            await _session.SaveChangesAsync().ConfigureAwait(false);

            using (var mgr = GetNewUserManager())
            {
                var returnedPhoneNumberConfirmed = await mgr.IsPhoneNumberConfirmedAsync(createdUserConfirmed.Id).ConfigureAwait(false);
                var returnedPhoneNumberNotConfirmed = await mgr.IsPhoneNumberConfirmedAsync(createdUserNotConfirmed.Id).ConfigureAwait(false);

                Assert.IsTrue(returnedPhoneNumberConfirmed);
                Assert.IsFalse(returnedPhoneNumberNotConfirmed);
            }
        }

        [Test]
        public async Task SetPhoneNumberConfirmedAsync_SetPhoneNumberConfirmed_IsSuccessful()
        {
            throw new NotImplementedException();
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

    }
}
*/
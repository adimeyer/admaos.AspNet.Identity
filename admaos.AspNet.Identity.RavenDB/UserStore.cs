using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Exceptions;
using Raven.Json.Linq;
using Raven.Client.Linq;
using Raven.Client.UniqueConstraints;

namespace admaos.AspNet.Identity.RavenDB
{

    // https://aspnetidentity.codeplex.com/SourceControl/latest#src/Microsoft.AspNet.Identity.EntityFramework/UserStore.cs
    // https://github.com/ILMServices/RavenDB.AspNet.Identity/blob/master/RavenDB.AspNet.Identity/UserStore.cs
    // https://github.com/tugberkugurlu/AspNet.Identity.RavenDB/blob/master/src/AspNet.Identity.RavenDB/Stores/RavenUserStore.cs

    /// <summary>
    /// UserStore Implementation for RavenDB
    /// </summary>

    public class UserStore<TUser> : IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser, string>,
        IUserTwoFactorStore<TUser, string>,
        IUserPhoneNumberStore<TUser>
        where TUser : IdentityUser
    {
        private bool _disposed;

        /// <summary>
        ///     RavenDB session
        /// </summary>
        public IAsyncDocumentSession Session { get; private set; }

        /// <summary>
        ///     If true will call dispose on the Session during Dispose
        /// </summary>
        public bool DisposeSession { get; set; }

        /// <summary>
        ///     If true will call SaveChanges after Create/Update/Delete
        /// </summary>
        public bool AutoSaveChanges { get; set; }

        /// <summary>
        ///     Constructor which takes a RavenDB session and wires up the stores with default instances using the context
        /// </summary>
        /// <param name="session"></param>
        public UserStore(IAsyncDocumentSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (session.Advanced.DocumentStore.Listeners.StoreListeners.All(sl => sl.GetType() != typeof (UniqueConstraintsStoreListener)))
            {
                throw new InvalidOperationException("UniqueConstraintStoreListener has not been registered");
            }
            //TODO: Check for Unique Constraint Bundle on Server
            DisposeSession = true;
            Session = session;
            AutoSaveChanges = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     If disposing, calls dispose on the Session.  Always nulls out the Session
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (DisposeSession && disposing)
            {
                Session?.Dispose();
            }
            _disposed = true;
            Session = null;
        }

        /// <summary>
        /// Throws an exception if already disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Only call SaveChanges if AutoSaveChanges is true
        /// </summary>
        private async Task SaveChangesAsync()
        {
            if (AutoSaveChanges)
            {
                await Session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        #region IUserStore

        /// <summary>
        /// Insert a new user
        /// </summary>
        /// <param name="user"/>
        public async Task CreateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            await Session.StoreAsync(user).ConfigureAwait(false);
            await SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Update a user
        /// </summary>
        /// <param name="user"/>
        /// <remarks>
        /// This method assumes that incomming TUser parameter is tracked in the session. So, this method literally behaves as SaveChangeAsync
        /// </remarks>
        public async Task UpdateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            await SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="user"/>
        public async Task DeleteAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            Session.Delete(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Finds a user
        /// </summary>
        /// <param name="userId"/>
        public virtual async Task<TUser> FindByIdAsync(string userId)
        {
            ThrowIfDisposed();
            return await Session.LoadAsync<TUser>(userId).ConfigureAwait(false);
        }

        /// <summary>
        /// Find a user by name
        /// </summary>
        /// <param name="userName"/>
        public virtual async Task<TUser> FindByNameAsync(string userName)
        {
            ThrowIfDisposed();
            var userLookup =
                await
                    Session.Query<TUser>()
                        .Where(u => u.UserName == userName)
                        .Customize(c => c.WaitForNonStaleResultsAsOfLastWrite())
                        .ToListAsync()
                        .ConfigureAwait(false);

            //TODO: remove as soon as uniqueness is enforced
            if (userLookup.Count > 1)
            {
                throw new NonUniqueObjectException("More than one user found with same userName");
            }

            return userLookup.SingleOrDefault();
        }

        #endregion

        #region IUserPasswordStore

        /// <summary>
        /// Set the user password hash
        /// </summary>
        /// <param name="user"/><param name="passwordHash"/>
        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Get the user password hash
        /// </summary>
        /// <param name="user"/>
        public Task<string> GetPasswordHashAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PasswordHash);
        }

        /// <summary>
        /// Returns true if a user has a password set
        /// </summary>
        /// <param name="user"/>
        public Task<bool> HasPasswordAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            { 
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PasswordHash != null);
        }

        #endregion

        #region IUserLoginStore

        /// <summary>
        /// Adds a user login with the specified provider and key
        /// </summary>
        /// <param name="user"/><param name="login"/>
        public Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            { 
                throw new ArgumentNullException(nameof(user));
            }
            if (!user.Logins.Any(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
            {
                user.Logins.Add(login);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Removes the user login with the specified combination if it exists
        /// </summary>
        /// <param name="user"/><param name="login"/>
        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.Logins.RemoveAll(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the linked accounts for this user
        /// </summary>
        /// <param name="user"/>
        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            { 
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult<IList<UserLoginInfo>>(user.Logins);
        }

        /// <summary>
        /// Returns the user associated with this login
        /// </summary>
        public async Task<TUser> FindAsync(UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }
            var userLookup = await Session.Query<TUser>()
                .Where(
                    u => u.Logins.Any(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
                .Customize(c => c.WaitForNonStaleResultsAsOfLastWrite())
                .ToListAsync().ConfigureAwait(false);

            //TODO: remove as soon as uniqueness is enforced
            if (userLookup.Count > 1)
            {
                throw new NonUniqueObjectException("More than one userLogin found with same LoginProvider && ProviderKey");
            }

            return userLookup.SingleOrDefault();
        }

        #endregion

        #region IClaimStore

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult<IList<Claim>>(user.Claims.Select(c => new Claim(c.Type, c.Value)).ToList());
        }

        public Task AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            if (!user.Claims.Any(uc => uc.Type == claim.Type && uc.Value == claim.Value))
            {
                user.Claims.Add(new IdentityUserClaim
                {
                    Type = claim.Type,
                    Value = claim.Value
                });
            }

            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            user.Claims.RemoveAll(uc => uc.Type == claim.Type && uc.Value == claim.Value);
            return Task.FromResult(0);
        }

        #endregion

        #region IUserRoleStore

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        public Task AddToRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }

            if (user.Roles.All(r => r != roleName))
            {
                user.Roles.Add(roleName);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Removes the role for the user
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        public Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }
            user.Roles.RemoveAll(r => r == roleName);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the roles for this user
        /// </summary>
        /// <param name="user"/>
        /// <returns/>
        public Task<IList<string>> GetRolesAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult<IList<string>>(user.Roles);
        }

        /// <summary>
        /// Returns true if a user is in the role
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        public Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }
            return Task.FromResult(user.Roles.Contains(roleName));
        }

        #endregion

        #region IUserSecurityStampStore

        /// <summary>
        /// Set the security stamp for the user
        /// </summary>
        /// <param name="user"/><param name="stamp"/>
        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentNullException(nameof(stamp));
            }
            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Get the user security stamp
        /// </summary>
        /// <param name="user"/>
        public Task<string> GetSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.SecurityStamp);
        }

        #endregion

        #region IUserEmailStore

        /// <summary>
        /// Set the user email
        /// </summary>
        /// <param name="user"/><param name="email"/>
        public Task SetEmailAsync(TUser user, string email)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentNullException(nameof(email));
            }
            user.Email = email;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Get the user email
        /// </summary>
        /// <param name="user"/>
        public Task<string> GetEmailAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.Email);
        }

        /// <summary>
        /// Returns true if the user email is confirmed
        /// </summary>
        /// <param name="user"/>
        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.EmailConfirmed);
        }

        /// <summary>
        /// Sets whether the user email is confirmed
        /// </summary>
        /// <param name="user"/><param name="confirmed"/>
        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the user associated with this email
        /// </summary>
        /// <param name="email"/>
        public async Task<TUser> FindByEmailAsync(string email)
        {
            ThrowIfDisposed();
            if (String.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            var userLookup =
                await
                    Session.Query<TUser>()
                        .Where(u => u.Email == email)
                        .Customize(c => c.WaitForNonStaleResultsAsOfLastWrite())
                        .ToListAsync()
                        .ConfigureAwait(false);

            //TODO: remove as soon as uniqueness is enforced
            if (userLookup.Count > 1)
            {
                throw new NonUniqueObjectException("More than one user found with the same emailAddress");
            }

            return userLookup.SingleOrDefault();
        }

        #endregion

        #region IUserLockoutStore

        /// <summary>
        /// Returns the DateTimeOffset that represents the end of a user's lockout, any time in the past should be considered
        ///                 not locked out.
        /// </summary>
        /// <param name="user"/>
        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return
                Task.FromResult(user.LockoutEndDateUtc.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc))
                    : new DateTimeOffset());
        }

        /// <summary>
        /// Locks a user out until the specified end date (set to a past date, to unlock a user)
        /// </summary>
        /// <param name="user"/><param name="lockoutEnd"/>
        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (lockoutEnd == null)
            {
                throw new ArgumentNullException(nameof(lockoutEnd));
            }
            user.LockoutEndDateUtc = lockoutEnd == DateTimeOffset.MinValue ? (DateTime?)null : lockoutEnd.UtcDateTime;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Used to record when an attempt to access the user has failed
        /// </summary>
        /// <param name="user"/>
        public async Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var patchRequestInc = new PatchRequest
            {
                Type = PatchCommandType.Inc,
                Name = nameof(user.AccessFailedCount),
                Value = new RavenJValue(1)
            };
            await Session.Advanced.DocumentStore.AsyncDatabaseCommands.ForDatabase(((InMemoryDocumentSessionOperations) Session).DatabaseName)
                .PatchAsync(Session.Advanced.GetDocumentId(user), new[]{patchRequestInc}).ConfigureAwait(false);
            await Session.Advanced.RefreshAsync(user).ConfigureAwait(false);
            return user.AccessFailedCount;
        }

        /// <summary>
        /// Used to reset the access failed count, typically after the account is successfully accessed
        /// </summary>
        /// <param name="user"/>
        public Task ResetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.AccessFailedCount = 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the current number of failed access attempts.  This number usually will be reset whenever the password is
        ///                 verified or the account is locked out.
        /// </summary>
        /// <param name="user"/>
        /// <returns/>
        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>
        /// Returns whether the user can be locked out.
        /// </summary>
        /// <param name="user"/>
        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.LockoutEnabled);
        }

        /// <summary>
        /// Sets whether the user can be locked out.
        /// </summary>
        /// <param name="user"/><param name="enabled"/>
        public Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.LockoutEnabled = enabled;
            return Task.FromResult(0);
        }

        #endregion

        #region IUserTwoFactorStore

        /// <summary>
        /// Sets whether two factor authentication is enabled for the user
        /// </summary>
        /// <param name="user"/><param name="enabled"/>
        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns whether two factor authentication is enabled for the user
        /// </summary>
        /// <param name="user"/>
        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.TwoFactorEnabled);
        }

        #endregion

        #region IUserPhoneNumberStore

        /// <summary>
        /// Set the user's phone number
        /// </summary>
        /// <param name="user"/><param name="phoneNumber"/>
        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (String.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Get the user phone number
        /// </summary>
        /// <param name="user"/>
        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PhoneNumber);
        }

        /// <summary>
        /// Returns true if the user phone number is confirmed
        /// </summary>
        /// <param name="user"/>
        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        /// <summary>
        /// Sets whether the user phone number is confirmed
        /// </summary>
        /// <param name="user"/><param name="confirmed"/>
        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        #endregion
    }
}
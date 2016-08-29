using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Raven.Client.UniqueConstraints;

namespace admaos.AspNet.Identity.RavenDB
{

    // https://aspnetidentity.codeplex.com/SourceControl/latest#src/Microsoft.AspNet.Identity.EntityFramework/IdentityUser.cs
    // https://github.com/ILMServices/RavenDB.AspNet.Identity/blob/master/RavenDB.AspNet.Identity/IdentityUser.cs
    // https://github.com/tugberkugurlu/AspNet.Identity.RavenDB/blob/master/src/AspNet.Identity.RavenDB/Entities/RavenUser.cs

    /// <summary>
    ///     IUser implementation
    /// </summary>
    public class IdentityUser : IUser
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public IdentityUser()
        {
            Claims = new List<Claim>();
            Roles = new List<string>();
            Logins = new List<UserLoginInfo>();
        }

        /// <summary>
        ///     Constructor that takes an id and a userName
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userName"></param>
        public IdentityUser(string id, string userName)
            : this()
        {
            Id = id;
            UserName = userName;
        }

        /// <summary>
        ///     Email
        /// </summary>
        [UniqueConstraint]
        public string Email { get; set; }

        /// <summary>
        ///     True if the email is confirmed, default is false
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        ///     The salted/hashed form of the user password
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        ///     A random value that should change whenever a users credentials have changed (password changed, login removed)
        /// </summary>
        public string SecurityStamp { get; set; }

        /// <summary>
        ///     PhoneNumber for the user
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        ///     True if the phone number is confirmed, default is false
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        ///     Is two factor enabled for the user
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        ///     DateTime in UTC when lockout ends, any time in the past is considered not locked out.
        /// </summary>
        public DateTime? LockoutEndDateUtc { get; set; }

        /// <summary>
        ///     Is lockout enabled for this user
        /// </summary>
        public bool LockoutEnabled { get; set; }

        /// <summary>
        ///     Used to record failures for the purposes of lockout
        /// </summary>
        public int AccessFailedCount { get; set; }

        /// <summary>
        ///     User roles
        /// </summary>
        public List<string> Roles { get; private set; }

        /// <summary>
        ///     User claims
        /// </summary>
        public List<Claim> Claims { get; protected set; }

        /// <summary>
        ///     Logins (facebook, twitter, etc)
        /// </summary>
        public List<UserLoginInfo> Logins { get; private set; }

        /// <summary>
        ///     User ID (unique id)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     User name
        /// </summary>
        [UniqueConstraint]
        public string UserName { get; set; }
    }
}
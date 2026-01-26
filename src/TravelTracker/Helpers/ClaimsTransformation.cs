//-----------------------------------------------------------------------
// <copyright file="MyClaimsTransformation.cs" company="Luppes Consulting, Inc.">
// Copyright 2025, Luppes Consulting, Inc. All rights reserved.
// </copyright>
// <summary>
// Claims Transformation Module
// </summary>
//-----------------------------------------------------------------------
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace TravelTracker.Helpers;

/// <summary>
/// Claims Transformation Module
/// </summary>
[ExcludeFromCodeCoverage]
public class MyClaimsTransformation : IClaimsTransformation
{
    /// <summary>
    /// Transform the claims to add an isAdmin claim and an Admin role membership
    /// </summary>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimsIdentity = new ClaimsIdentity();
        if (principal != null && principal.Identity != null)
        {
            claimsIdentity = new ClaimsIdentity(principal.Identity);
            // <hack> Yes... this is a hack, but it works for this simple example. This should be replaced by actual Active Directory roles. </hack>
            var adminUserList = "lyleluppes@microsoft.com,lyle@luppes.com,lyle.luppes@gmail.com";
            // var adminUserList = Configuration["AppSettings:AdminUserList"];
            var isAdmin =
                !string.IsNullOrEmpty(adminUserList) &&
                adminUserList.Contains(claimsIdentity.Name, StringComparison.InvariantCultureIgnoreCase);

            if (isAdmin && !principal.IsInRole("Admin"))
            {
                claimsIdentity.AddClaim(new Claim(claimsIdentity.RoleClaimType, "Admin"));
            }

            //if (isAdmin && !principal.HasClaim(claim => claim.Type == Data.Constants.Security.AdminClaimType))
            //{
            //    claimsIdentity.AddClaim(new Claim(Data.Constants.Security.AdminClaimType, "true"));
            //}
        }

        principal.AddIdentity(claimsIdentity);
        return Task.FromResult(principal);
    }
}
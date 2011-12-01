﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rock
{
    /// <summary>
    /// Extension Methods
    /// </summary>
    public static class ExtensionMethods
    {
        #region String Extensions

        /// <summary>
        /// Splits a Camel or Pascal cased identifier into seperate words.
        /// </summary>
        /// <param name="str">The identifier.</param>
        /// <returns></returns>
        public static string SplitCase( this string str )
        {
            return Regex.Replace( Regex.Replace( str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2" ), @"(\p{Ll})(\P{Ll})", "$1 $2" );
        }

        #endregion

        #region MembershipUser Extensions

        /// <summary>
        /// Returns the PersonId associated with the <see cref="System.Web.Security.MembershipUser"/> object
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        public static int? PersonId( this System.Web.Security.MembershipUser user )
        {
            if ( user.ProviderUserKey != null )
                return ( int )user.ProviderUserKey;
            else
                return null;
        }

        #endregion

        #region Enum Extensions

        public static String ConvertToString( this Enum eff )
        {
            return Enum.GetName( eff.GetType(), eff );
        }

        public static T ConvertToEnum<T>( this String enumValue )
        {
            return ( T )Enum.Parse( typeof( T ), enumValue );
        }

        #endregion

    }
}
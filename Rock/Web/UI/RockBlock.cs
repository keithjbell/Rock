﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Web.UI
{
    /// <summary>
    /// RockBlock is the base abstract class that all Blocks should inherit from
    /// </summary>
    public abstract class RockBlock : UserControl
    {
        #region Private Properties

        internal BlockCache _blockCache;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the <see cref="Rock.Web.UI.RockPage">page</see> that contains the block (instance).
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Web.UI.RockPage"/> that contains this block (instance).
        /// </value>
        public RockPage RockPage
        {
            get
            {
                return (RockPage)this.Page;
            }
        }

        /// <summary>
        /// Gets the BlockId of this <see cref="Rock.Model.Block"/>.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the BlockId of this <see cref="Rock.Model.Block"/>.
        /// </value>
        public int BlockId
        {
            get { return _blockCache.Id; }
        }

        /// <summary>
        /// Gets the name of the block.
        /// </summary>
        /// <value>
        /// The name of the block.
        /// </value>
        public string BlockName
        {
            get { return _blockCache.Name; }
        }

        /// <summary>
        /// Gets the current page reference.
        /// </summary>
        public PageReference CurrentPageReference
        {
            get { return RockPage.PageReference; }
            set { RockPage.PageReference = value; }
        }

        /// <summary>
        /// The personID of the currently logged in user.  If user is not logged in, returns null
        /// </summary>
        public int? CurrentPersonId
        {
            get { return RockPage.CurrentPersonId; }
        }
        /// <summary>
        /// Gets the current person alias.
        /// </summary>
        public PersonAlias CurrentPersonAlias
        {
            get { return RockPage.CurrentPersonAlias; }
        }

        /// <summary>
        /// Gets the current person alias identifier.
        /// </summary>
        /// <value>
        /// The current person alias identifier.
        /// </value>
        public int? CurrentPersonAliasId
        {
            get { return RockPage.CurrentPersonAliasId; }
        }

        /// <summary>
        /// Returns the currently logged in user.  If user is not logged in, returns null
        /// </summary>
        public UserLogin CurrentUser
        {
            get { return RockPage.CurrentUser; }
        }

        /// <summary>
        /// Returns the currently logged in person. If user is not logged in, returns null
        /// </summary>
        public Person CurrentPerson
        {
            get { return RockPage.CurrentPerson; }
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string BlockValidationGroup { get; set; }

        /// <summary>
        /// Gets the bread crumbs that were created during the page's oninit.  A block
        /// can add additional breadcrumbs to this list to be rendered.  Crumb's added 
        /// this way will not be saved to the current page reference's collection of 
        /// breadcrumbs, so wil not be available when user navigates to another child
        /// page.  Because of this only last-level crumbs should be added this way.  To
        /// persist breadcrumbs in the session state, override the GetBreadCrumbs 
        /// method instead.
        /// </summary>
        /// <value>
        /// The bread crumbs.
        /// </value>
        public List<BreadCrumb> BreadCrumbs
        {
            get { return RockPage.BreadCrumbs; }
        }

        /// <summary>
        /// Gets the root URL Path.
        /// </summary>
        public string RootPath
        {
            get
            {
                Uri uri = new Uri( HttpContext.Current.Request.Url.ToString() );
                return uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped ) + Page.ResolveUrl( "~" );
            }
        }

        /// <summary>
        /// Gets a list of any context entities that the block requires.
        /// </summary>
        public virtual List<EntityTypeCache> ContextTypesRequired
        {
            get
            {
                if ( _contextTypesRequired == null )
                {
                    _contextTypesRequired = new List<EntityTypeCache>();

                    int properties = 0;
                    foreach ( var attribute in this.GetType().GetCustomAttributes( typeof( ContextAwareAttribute ), true ) )
                    {
                        var contextAttribute = (ContextAwareAttribute)attribute;
                        var entityType = contextAttribute.EntityType;

                        if ( contextAttribute.EntityType == null )
                        {
                            // If the entity type was not specified in the attibute, look for a property that defines it
                            string propertyKeyName = string.Format( "ContextEntityType{0}", properties > 0 ? properties.ToString() : "" );
                            properties++;

                            Guid guid = Guid.Empty;
                            if ( Guid.TryParse( GetAttributeValue( propertyKeyName ), out guid ) )
                            {
                                entityType = EntityTypeCache.Read( guid );
                            }
                        }

                        if ( entityType != null && !_contextTypesRequired.Any( e => e.Guid.Equals( entityType.Guid ) ) )
                        {
                            _contextTypesRequired.Add( entityType );
                        }
                        else
                        {
                            if ( !contextAttribute.IsConfigurable )
                            {
                                // block support any ContextType of any entityType, and it isn't configurable in BlockPropties, so load all the ones that RockPage knows about
                                _contextTypesRequired = RockPage.GetContextEntityTypes();
                            }
                        }
                    }
                    
                }
                return _contextTypesRequired;
            }
        }
        private List<EntityTypeCache> _contextTypesRequired;

        /// <summary>
        /// Gets a dictionary of the current context entities.  The key is the type of context, and the value is the entity object
        /// </summary>
        /// <value>
        /// The context entities.
        /// </value>
        private Dictionary<string, Rock.Data.IEntity> ContextEntities { get; set; }

        /// <summary>
        /// Returns the ContextEntity of the Type specified
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ContextEntity<T>() where T : Rock.Data.IEntity
        {
            IEntity entity = ContextEntity( typeof( T ).FullName );
            if ( entity != null )
            {
                return (T)entity;
            }
            else
            {
                return default( T );
            }
        }

        /// <summary>
        /// Returns the ContextEntity of the entityType specified
        /// </summary>
        /// <param name="entityTypeName">Name of the entity type.  For example: Rock.Model.Campus </param>
        /// <returns></returns>
        public Rock.Data.IEntity ContextEntity(string entityTypeName)
        {
            if ( ContextEntities.ContainsKey( entityTypeName ) )
            {
                return ContextEntities[entityTypeName];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Return the ContextEntity for blocks that are designed to have at most one ContextEntity
        /// </summary>
        /// <returns></returns>
        public Rock.Data.IEntity ContextEntity()
        {
            if ( ContextEntities.Count() == 1 )
            {
                return ContextEntities.First().Value;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RockBlock" /> class.
        /// </summary>
        public RockBlock()
        {
        }

        #endregion

        #region Protected Caching Methods

        /// <summary>
        /// Adds an object to the default <see cref="System.Runtime.Caching.MemoryCache"/> .
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to cache</param>
        protected virtual void AddCacheItem( object value )
        {
            CacheItemPolicy cacheItemPolicy = null;
            AddCacheItem( string.Empty, value, cacheItemPolicy );
        }

        /// <summary>
        /// Adds a keyed/named object to the default <see cref="System.Runtime.Caching.MemoryCache"/> .
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the name of the key to differentiate items from same block instance</param>
        /// <param name="value">The <see cref="System.Object"/> to cache.</param>
        protected virtual void AddCacheItem( string key, object value )
        {
            CacheItemPolicy cacheItemPolicy = null;
            AddCacheItem( key, value, cacheItemPolicy );
        }

        /// <summary>
        /// Adds an object to the default <see cref="System.Runtime.Caching.MemoryCache"/>  for a specified amount of time.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the name of the key to differentiate items from same block instance</param>
        /// <param name="value">The <see cref="System.Object"/> to cache.</param>
        /// <param name="seconds">A <see cref="System.Int32"/> representing the the amount of time in seconds that the object is cached. This is an absolute expiration</param>
        protected virtual void AddCacheItem( string key, object value, int seconds )
        {
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds( seconds );
            AddCacheItem( key, value, cacheItemPolicy );
        }

        /// <summary>
        /// Adds an object with a <see cref="System.Runtime.Caching.CacheItemPolicy"/> to the default <see cref="System.Runtime.Caching.MemoryCache"/> 
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the name of the key to differentiate items from same block instance</param>
        /// <param name="value">The <see cref="System.Object"/> to cache.</param>
        /// <param name="cacheItemPolicy">Optional <see cref="System.Runtime.Caching.CacheItemPolicy"/>, defaults to null</param>
        protected virtual void AddCacheItem( string key, object value, CacheItemPolicy cacheItemPolicy )
        {
            ObjectCache cache = RockMemoryCache.Default;
            cache.Set( ItemCacheKey( key ), value, cacheItemPolicy );
        }

        /// <summary>
        /// Returns an object from the default <see cref="System.Runtime.Caching.MemoryCache"/> .
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the name of the object's key. Defaults to an empty string.</param>
        /// <returns>The cached <see cref="System.Object"/> if a key match is not found, a null object will be returned.</returns>
        protected virtual object GetCacheItem( string key = "" )
        {
            ObjectCache cache = RockMemoryCache.Default;
            return cache[ItemCacheKey( key )];
        }

        /// <summary>
        /// Flushes an object from the cache.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the key name for the item that will be flushed. This value 
        /// defaults to an empty string.</param>
        protected virtual void FlushCacheItem( string key = "" )
        {
            ObjectCache cache = RockMemoryCache.Default;
            cache.Remove( ItemCacheKey( key ) );
        }

        /// <summary>
        /// Flushes a block from all places in the cache (layouts, pages, etc.).
        /// NOTE: Retrieving an enumerator for a MemoryCache instance is a resource-intensive and blocking operation. 
        /// Therefore, it should not be used in production applications (if possible).
        /// </summary>
        /// <param name="blockId">An <see cref="System.Int32"/> representing the block item that will be flushed.</param>
        protected virtual void FlushSharedBlock( int blockId )
        {
            MemoryCache cache = RockMemoryCache.Default;
            string blockKey = string.Format( ":RockBlock:{0}:", blockId );
            foreach ( var keyValuePair in cache.Where( k => k.Key.Contains( blockKey ) ) )
            {
                cache.Remove( keyValuePair.Key);
            }
        }

        /// <summary>
        /// Returns the qualified key name for the cached item. The format is PageID:BlockID:Key.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the base key name.</param>
        /// <returns>A <see cref="System.String" /> representing the fully qualified key name.</returns>
        private string ItemCacheKey( string key )
        {
            string cacheKeyTemplate = "Rock:{0}:{1}:RockBlock:{2}:ItemCache:{3}";

            if (_blockCache.PageId.HasValue)
            {
                return string.Format( cacheKeyTemplate, "Page", _blockCache.PageId.Value, _blockCache.Id, key );
            }
            else
            {
                return string.Format( cacheKeyTemplate, "Layout", (_blockCache.LayoutId ?? 0), _blockCache.Id, key );
            }
        }

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            // Get the context types defined through configuration or block properties
            var requiredContext = ContextTypesRequired;

            // Check to see if a context type was specified in a query, form, or page route parameter
            string param = PageParameter( "ContextEntityType" );
            if ( !String.IsNullOrWhiteSpace( param ) )
            {
                var entityType = EntityTypeCache.Read( param, false );
                if ( entityType != null )
                {
                    requiredContext.Add( entityType );
                }
            }

            // Get the current context for each required context type
            ContextEntities = new Dictionary<string, Data.IEntity>();
            foreach ( var contextEntityType in requiredContext )
            {
                Data.IEntity contextEntity = RockPage.GetCurrentContext( contextEntityType );
                if ( contextEntity != null )
                {
                    ContextEntities.AddOrReplace( contextEntityType.Name, contextEntity );
                }
            }

            base.OnInit( e );

            this.BlockValidationGroup = string.Format( "{0}_{1}", this.GetType().BaseType.Name, _blockCache.Id );

            RockPage.BlockUpdated += Page_BlockUpdated;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            SetValidationGroup( this.Controls, BlockValidationGroup );
        }

        /// <summary>
        /// When a control renders it's content to the page, this method will also check to see if 
        /// the block instance of this control has been configured for output caching, and if so, 
        /// the contents will also be rendered to a string variable that will gets cached in the 
        /// default MemoryCache for use next time by the Rock.Web.UI.RockPage.OnInit() method when rendering the 
        /// control content.
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render( HtmlTextWriter writer )
        {
            string preHtml = string.Empty;
            string postHtml = string.Empty;

            string appRoot = ResolveRockUrl( "~/" );
            string themeRoot = ResolveRockUrl( "~~/" );

            if ( Visible )
            {
                if ( !string.IsNullOrWhiteSpace( _blockCache.PreHtml ) )
                {
                    preHtml = _blockCache.PreHtml.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                    var preHtmlControl = this.FindControl( "lPreHtml" ) as Literal;
                    if ( preHtmlControl != null )
                    {
                        preHtmlControl.Text = preHtml;
                        preHtml = string.Empty;
                    }
                }

                if ( !string.IsNullOrWhiteSpace( _blockCache.PostHtml ) )
                {
                    postHtml = _blockCache.PostHtml.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                    var postHtmlControl = this.FindControl( "lPostHtml" ) as Literal;
                    if ( postHtmlControl != null )
                    {
                        postHtmlControl.Text = postHtml;
                        postHtml = string.Empty;
                    }
                }
            }

            if ( _blockCache.OutputCacheDuration > 0 )
            {
                string blockCacheKey = string.Format( "Rock:BlockOutput:{0}", _blockCache.Id );
                StringBuilder sbOutput = new StringBuilder();
                StringWriter swOutput = new StringWriter( sbOutput );
                HtmlTextWriter twOutput = new HtmlTextWriter( swOutput );

                base.Render( twOutput );

                CacheItemPolicy cacheDuration = new CacheItemPolicy();
                cacheDuration.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds( _blockCache.OutputCacheDuration );

                ObjectCache cache = RockMemoryCache.Default;
                cache.Set( blockCacheKey, sbOutput.ToString(), cacheDuration );
            }

            writer.Write( preHtml );
            base.Render( writer );
            writer.Write( postHtml );

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the block instance.
        /// </summary>
        /// <param name="blockCache">The block instance from <see cref="Rock.Web.Cache.BlockCache"/> .</param>
        public void SetBlock( BlockCache blockCache )
        {
            _blockCache = blockCache;
        }

        /// <summary>
        /// Saves the block attribute values.
        /// </summary>
        public void SaveAttributeValues()
        {
            if ( _blockCache != null )
            {
                _blockCache.SaveAttributeValues();
            }
        }

        /// <summary>
        /// Returns the current value for the block attribute for the specified key. If the attribute value is not found, a null value will be returned.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the key name for the block attribute to retrieve.</param>
        /// <returns>A <see cref="System.String"/> representing the stored attribute value. If the attribute is not found, this value will be null.</returns>
        public string GetAttributeValue( string key )
        {
            if ( _blockCache != null )
            {
                return _blockCache.GetAttributeValue( key );
            }
            return null;
        }

        /// <summary>
        /// Returns a <see cref="System.Collections.Generic.List{String}"/> of the current block attribute values for the specified key. If the key is not 
        /// found an empty list will be returned.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> representing the block attribute key</param>
        /// <returns>A <see cref="System.Collections.Generic.List{String}"/> containing the current attribute values for the specified key. If the key is not
        /// found, an empty list will be returned.</returns>
        public List<string> GetAttributeValues( string key )
        {
            if ( _blockCache != null )
            {
                return _blockCache.GetAttributeValues( key );
            }

            return new List<string>();
        }

        /// <summary>
        /// Sets the value of an block attribute key in memory. Once values have been set, use the <see cref="SaveAttributeValues()" /> method to save all values to database
        /// </summary>
        /// <param name="key">A <see cref="System.String" /> representing the block attribute's key name.</param>
        /// <param name="value">A <see cref="System.String" /> representing the value of the attribute.</param>
        public void SetAttributeValue( string key, string value )
        {
            if ( _blockCache != null )
            {
                _blockCache.SetAttributeValue( key, value );
            }
        }

        /// <summary>
        /// Adds an update trigger for when the block properties are updated.
        /// </summary>
        /// <param name="updatePanel">The <see cref="System.Web.UI.UpdatePanel"/> that is being added.</param>
        public void AddConfigurationUpdateTrigger( UpdatePanel updatePanel )
        {
            RockPage.AddConfigurationUpdateTrigger( updatePanel );
        }

        /// <summary>
        /// Evaluates if the CurrentPerson is authorized to perform the requested action.
        /// </summary>
        /// <param name="action">A <see cref="System.String" /> representing the action that the <see cref="Rock.Model.UserLogin"/>/<see cref="CurrentPerson"/> 
        /// is requesting to perform.</param>
        /// <returns>A <see cref="System.Boolean"/> that is <c>true</c> if the CurrentPerson is authorized to perform the requested action; otherwise <c>false</c>.</returns>
        public bool IsUserAuthorized( string action )
        {
            return _blockCache.IsAuthorized( action, CurrentPerson );
        }

        /// <summary>
        /// Returns the specified page parameter value.  The <see cref="Rock.Model.Page">page's</see> <see cref="Rock.Model.PageRoute"/>
        /// is checked first and then query string values.  If a match is not found an empty string is returned.
        /// </summary>
        /// <param name="name">A <see cref="System.String"/> representing the name of the specified page parameter.</param>
        /// <returns>A <see cref="System.String"/> representing the value of the page parameter. If a match is not found, an empty string is returned.</returns>
        public string PageParameter( string name )
        {
            return RockPage.PageParameter( name );
        }

        /// <summary>
        /// Returns a specified page parameter from the specified <see cref="Rock.Web.PageReference"/>. If a match is not found,
        /// an empty string is returned.
        /// </summary>
        /// <param name="pageReference">The <see cref="Rock.Web.PageReference"/></param>
        /// <param name="name">A <see cref="System.String" /> representing the name of the page parameter.</param>
        /// <returns>A <see cref="System.String"/> representing the page parameter value. If match is not found, an empty string will be returned.</returns>
        public string PageParameter( PageReference pageReference, string name )
        {
            return RockPage.PageParameter( pageReference, name );
        }

        /// <summary>
        /// Returns a <see cref="System.Collections.Generic.Dictionary{String, Object}" /> representing all of the <see cref="Rock.Model.Page">page's</see> page parameters.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Collections.Generic.Dictionary{String, Obejct}"/> containing all the <see cref="Rock.Model.Page">page's</see> page parameters. Each 
        /// <see cref="System.Collections.Generic.KeyValuePair{String, Object}"/> consists of the key being a <see cref="System.String"/> representing
        /// the name of the page parameter and the value being an <see cref="System.Object"/> that represents the parameter value.
        /// </returns>
        public Dictionary<string, object> PageParameters()
        {
            return RockPage.PageParameters();
        }

        /// <summary>
        /// Builds and returns the URL for a linked <see cref="Rock.Model.Page"/> from a "linked page attribute" and any necessary query parameters.
        /// </summary>
        /// <param name="attributeKey">A <see cref="System.String"/> representing the name of the linked <see cref="Rock.Model.Page"/> attribute key.</param>
        /// <param name="queryParams">A <see cref="System.Collections.Generic.Dictionary{String,String}" /> containing the query string parameters to be added to the URL.  
        /// In each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> the key value is a <see cref="System.String"/> that represents the name of the query string 
        /// parameter, and the value is a <see cref="System.String"/> that represents the query string value..</param>
        /// <returns>A <see cref="System.String"/> representing the URL to the linked <see cref="Rock.Model.Page"/>. </returns>
        public string LinkedPageUrl( string attributeKey, Dictionary<string, string> queryParams = null )
        {
            var pageReference = new PageReference( GetAttributeValue( attributeKey ), queryParams );
            if ( pageReference.PageId > 0 )
            {
                return pageReference.BuildUrl();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Navigate to a linked <see cref="Rock.Model.Page"/>.
        /// </summary>
        /// <param name="attributeKey">A <see cref="System.String"/> representing the name of the linked <see cref="Rock.Model.Page"/> attribute key.</param>
        /// <param name="queryParams">A <see cref="System.Collections.Generic.Dictionary{String,String}"/> containing the query string parameters to include in the linked page URL.  
        /// Each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> the key value is a <see cref="System.String"/> that represents the name of the query string
        /// parameter, and the value is a <see cref="System.String"/> that represents the query string value. This dictionary defaults to a null value.</param>
        public void NavigateToLinkedPage( string attributeKey, Dictionary<string, string> queryParams = null )
        {
            string url = LinkedPageUrl( attributeKey, queryParams );

            // Verify valid url before redirecting (otherwise may get an 'Object moved to here' error in browser)
            if ( !string.IsNullOrWhiteSpace( url ) )
            {
                Response.Redirect( url, false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Navigates to a linked <see cref="Rock.Model.Page"/>
        /// </summary>
        /// <param name="attributeKey">A <see cref="System.String"/> representing the name of the linked <see cref="Rock.Model.Page"/> attribute key.</param>
        /// <param name="itemKey">A <see cref="System.String"/> representing the key name of the item that is being passed to the linked page in the query string. </param>
        /// <param name="itemKeyValue">A <see cref="System.Int32"/> representing the item value that is being passed to the link page in the query string.</param>
        /// <param name="itemParentKey">A <see cref="System.String"/> representing the key name of the parent item that is being passed to the linked page in the query string. 
        /// This value defaults to null.</param>
        /// <param name="itemParentValue">A <see cref="System.Int32"/> representing the parent item value that is being passed to the linked page in the query string. 
        /// This value defaults to null.</param>
        public void NavigateToLinkedPage( string attributeKey, string itemKey, int itemKeyValue, string itemParentKey = null, int? itemParentValue = null )
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add( itemKey, itemKeyValue.ToString() );
            if ( !string.IsNullOrWhiteSpace( itemParentKey ) )
            {
                queryParams.Add( itemParentKey, ( itemParentValue ?? 0 ).ToString() );
            }

            NavigateToLinkedPage( attributeKey, queryParams );
        }

        /// <summary>
        /// Navigates/redirects to the parent <see cref="Rock.Model.Page"/>.
        /// </summary>
        /// <param name="queryString">A <see cref="System.Collections.Generic.Dictionary{String,String}"/> containing the query string parameters to include in the linked <see cref="Rock.Model.Page"/> URL.  
        /// Each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> the key value is a <see cref="System.String"/> that represents the name of the query string
        /// parameter, and the value is a <see cref="System.String"/> that represents the query string value. This dictionary defaults to a null value.</param>
        public void NavigateToParentPage( Dictionary<string, string> queryString = null )
        {
            var pageCache = PageCache.Read( RockPage.PageId );
            if ( pageCache != null )
            {
                var parentPage = pageCache.ParentPage;
                if ( parentPage != null )
                {
                    NavigateToPage( parentPage.Guid, queryString );
                }
            }
        }

        /// <summary>
        /// Navigates to the <see cref="Rock.Model.Page"/> specified by the provided <see cref="System.Guid"/>.
        /// </summary>
        /// <param name="pageGuid">A <see cref="System.Guid"/> that represents the <see cref="Rock.Model.Page">Page's</see> unique identifier.</param>
        /// <param name="queryString">A <see cref="System.Collections.Generic.Dictionary{String,String}"/> containing the query string parameters to include in the linked page URL.  
        /// Each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> the key value is a <see cref="System.String"/> that represents the name of the query string
        /// parameter, and the value is a <see cref="System.String"/> that represents the query string value. This dictionary defaults to a null value.</param>
        public void NavigateToPage( Guid pageGuid, Dictionary<string, string> queryString )
        {
            NavigateToPage( pageGuid, Guid.Empty, queryString );
        }

        /// <summary>
        /// Navigates to the <see cref="Rock.Model.Page"/> specified by the provided <see cref="System.Guid">page Guid</see> using the <see cref="Rock.Model.PageRoute"/> specified by the 
        /// provided <see cref="System.Guid">page route Guid</see>.
        /// </summary>
        /// <param name="pageGuid">A <see cref="System.Guid"/> that represents the <see cref="Rock.Model.Page">Page's</see> unique identifier.</param>
        /// <param name="pageRouteGuid">A <see cref="System.Guid" /> that represents the <see cref="Rock.Model.PageRoute">PageRoute's</see> unique identifier.</param>
        /// <param name="queryString">A <see cref="System.Collections.Generic.Dictionary{String,String}"/> containing the query string parameters to include in the linked page URL.  
        /// Each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> the key value is a <see cref="System.String"/> that represents the name of the query string
        /// parameter, and the value is a <see cref="System.String"/> that represents the query string value. This dictionary defaults to a null value.</param>
        public void NavigateToPage( Guid pageGuid, Guid pageRouteGuid, Dictionary<string, string> queryString )
        {
            var pageCache = PageCache.Read( pageGuid );
            if ( pageCache != null )
            {
                int routeId = 0;
                {
                    var pageRouteInfo = pageCache.PageRoutes.FirstOrDefault( a => a.Guid == pageRouteGuid );
                    if ( pageRouteInfo != null )
                    {
                        routeId = pageRouteInfo.Id;
                    }
                }

                NavigateToPage( new PageReference( pageCache.Id, routeId, queryString, null ) );
            }
        }

        /// <summary>
        /// Navigates to page.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        public void NavigateToPage( PageReference pageReference )
        {
            string pageUrl = pageReference.BuildUrl();
            Response.Redirect( pageUrl, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Sets the visibility of the secondary blocks on the page
        /// </summary>
        /// <param name="hidden">A <see cref="System.Boolean"/> value that indicates if the secondary blocks should be hidden. If <c>true</c> then the secondary blocks will be
        /// hidden; otherwise <c>false</c> and the secondary blocks will be visible.</param>
        public void HideSecondaryBlocks( bool hidden )
        {
            RockPage.HideSecondaryBlocks( this, hidden );
        }

        /// <summary>
        /// Adds a history point to the <see cref="System.Web.UI.ScriptManager"/>.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> that represents the name of the key to use for the history point.</param>
        /// <param name="state">A <see cref="System.String"/> that represents any state information to store for the history point</param>
        /// <param name="title">A <see cref="System.String"/> that represents the page title to be used by the browser</param>
        public void AddHistory( string key, string state, string title = "" )
        {
            RockPage.AddHistory( key, state, title );
        }

        /// <summary>
        /// Resolves a rock URL.  Similar to the <see cref="System.Web.UI.Control" /> ResolveUrl method except that you can prefix
        /// a Url with '~~' to indicate a virtual path to Rock's current theme root folder
        /// </summary>
        /// <param name="url">A <see cref="System.String" /> representing the Url to resolve.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents the resolved Url.
        /// </returns>
        public string ResolveRockUrl( string url)
        {
            return RockPage.ResolveRockUrl( url );
        }

        /// <summary>
        /// Resolves the rock URL and includes root.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public string ResolveRockUrlIncludeRoot( string url )
        {
            return RockPage.ResolveRockUrlIncludeRoot ( url );
        }

        /// <summary>
        /// Sets the validation group.
        /// </summary>
        /// <param name="controls">A <see cref="System.Web.UI.ControlCollection"/> containing the controls to include in the validation group.</param>
        /// <param name="validationGroup">A <see cref="System.String"/> representing the name of the validation group.</param>
        public void SetValidationGroup( ControlCollection controls, string validationGroup )
        {
            if ( controls != null )
            {
                foreach ( Control control in controls )
                {
                    if ( control is Rock.Web.UI.Controls.IHasValidationGroup )
                    {
                        var rockControl = (Rock.Web.UI.Controls.IHasValidationGroup)control;
                        rockControl.ValidationGroup = SetValidationGroup( rockControl.ValidationGroup, validationGroup );
                    }

                    if ( control is ValidationSummary )
                    {
                        var validationSummary = (ValidationSummary)control;
                        validationSummary.ValidationGroup = SetValidationGroup( validationSummary.ValidationGroup, validationGroup );
                    }

                    else if ( control is BaseValidator )
                    {
                        var validator = (BaseValidator)control;
                        validator.ValidationGroup = SetValidationGroup( validator.ValidationGroup, validationGroup );
                    }

                    else if ( control is IButtonControl )
                    {
                        var button = (IButtonControl)control;
                        button.ValidationGroup = SetValidationGroup( button.ValidationGroup, validationGroup );
                    }
                    else
                    {
                        // Check child controls
                        SetValidationGroup( control.Controls, validationGroup );
                    }
                }
            }
        }

        #region User Preferences

        /// <summary>
        /// Returns the user preference value for the current user for a given key
        /// </summary>
        /// <param name="key">A <see cref="System.String" /> representing the key to the user preference.</param>
        /// <returns>A <see cref="System.String" /> representing the user preference value. If a match for the key is not found, 
        /// an empty string will be returned.</returns>
        public string GetUserPreference( string key )
        {
            return RockPage.GetUserPreference( key );
        }

        /// <summary>
        /// Gets the preferences for the current user where the key begins with the specified value.
        /// </summary>
        /// <param name="keyPrefix">A <see cref="System.String"/> representing the key preference. Any user preference
        /// for the current user that begins with this value will be returned.</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary{String,String}"/> that contains all user preferences for the current 
        /// user that begins with the key prefix.  Each <see cref="System.Collections.Generic.KeyValuePair{String,String}"/> includes 
        /// a key <see cref="System.String"/> that represents the user preference key and a value <see cref="System.String"/> that 
        /// represents the user preference value. If no preferences are found, an empty dictionary will be returned.</returns>
        public Dictionary<string, string> GetUserPreferences( string keyPrefix )
        {
            return RockPage.GetUserPreferences( keyPrefix );
        }

        /// <summary>
        /// Sets a user preference for the current user with the specified key and value.
        /// </summary>
        /// <param name="key">A <see cref="System.String"/> that represents the key value that identifies the 
        /// user preference.</param>
        /// <param name="value">A <see cref="System.String"/> that represents the value of the user preference.</param>
        public void SetUserPreference( string key, string value )
        {
            RockPage.SetUserPreference( key, value );
        }

        #endregion

        /// <summary>
        /// Adds icons to the configuration area of a <see cref="Rock.Model.Block"/> instance.  Can be overridden to
        /// add additional icons
        /// </summary>
        /// <param name="canConfig">A <see cref="System.Boolean" /> flag that indicates if the user can configure the <see cref="Rock.Model.Block"/> instance.
        /// This value will be <c>true</c> if the user is allowed to configure the <see cref="Rock.Model.Block"/> instance; otherwise <c>false</c>.</param>
        /// <param name="canEdit">A <see cref="System.Boolean"/> flag that indicates if the user can edit the <see cref="Rock.Model.Block"/> instance. 
        /// This value will be <c>true</c> if the user is allowed to edit the <see cref="Rock.Model.Block"/> instance; otherwise <c>false</c>.</param>
        /// <returns>A <see cref="System.Collections.Generic.List{Control}" /> containing all the icon <see cref="System.Web.UI.Control">controls</see> 
        /// that will be available to the user in the configuration area of the block instance.</returns>
        public virtual List<Control> GetAdministrateControls( bool canConfig, bool canEdit )
        {
            List<Control> configControls = new List<Control>();

            if ( canConfig )
            {
                // Icon to display block properties
                HtmlGenericControl aAttributes = new HtmlGenericControl( "a" );
                aAttributes.ID = "aBlockProperties";
                aAttributes.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                aAttributes.Attributes.Add( "class", "properties" );
                aAttributes.Attributes.Add( "height", "500px" );
                aAttributes.Attributes.Add( "href", "javascript: Rock.controls.modal.show($(this), '" + ResolveUrl( string.Format( "~/BlockProperties/{0}?t=Block Properties", _blockCache.Id ) ) + "')" );
                aAttributes.Attributes.Add( "title", "Block Properties" );
                //aAttributes.Attributes.Add( "instance-id", BlockInstance.Id.ToString() );
                configControls.Add( aAttributes );
                HtmlGenericControl iAttributes = new HtmlGenericControl( "i" );
                aAttributes.Controls.Add( iAttributes );
                iAttributes.Attributes.Add( "class", "fa fa-cog" );

                // Security
                HtmlGenericControl aSecureBlock = new HtmlGenericControl( "a" );
                aSecureBlock.ID = "aSecureBlock";
                aSecureBlock.ClientIDMode = System.Web.UI.ClientIDMode.Static;
                aSecureBlock.Attributes.Add( "class", "security" );
                aSecureBlock.Attributes.Add( "height", "500px" );
                aSecureBlock.Attributes.Add( "href", "javascript: Rock.controls.modal.show($(this), '" + ResolveUrl( string.Format( "~/Secure/{0}/{1}?t=Block Security&pb=&sb=Done",
                    EntityTypeCache.Read( typeof( Block ) ).Id, _blockCache.Id ) ) + "')" );
                aSecureBlock.Attributes.Add( "title", "Block Security" );
                configControls.Add( aSecureBlock );
                HtmlGenericControl iSecureBlock = new HtmlGenericControl( "i" );
                aSecureBlock.Controls.Add( iSecureBlock );
                iSecureBlock.Attributes.Add( "class", "fa fa-lock" );

                var pageCache = PageCache.Read( RockPage.PageId );
                if ( pageCache.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) )
                {
                    // Move
                    HtmlGenericControl aMoveBlock = new HtmlGenericControl( "a" );
                    aMoveBlock.Attributes.Add( "class", "block-move block-move" );
                    aMoveBlock.Attributes.Add( "href", _blockCache.Id.ToString() );
                    aMoveBlock.Attributes.Add( "data-zone", _blockCache.Zone );
                    aMoveBlock.Attributes.Add( "data-zone-location", _blockCache.BlockLocation.ToString() );
                    aMoveBlock.Attributes.Add( "title", "Move Block" );
                    configControls.Add( aMoveBlock );
                    HtmlGenericControl iMoveBlock = new HtmlGenericControl( "i" );
                    aMoveBlock.Controls.Add( iMoveBlock );
                    iMoveBlock.Attributes.Add( "class", "fa fa-external-link" );
                }

                // Delete
                HtmlGenericControl aDeleteBlock = new HtmlGenericControl( "a" );
                aDeleteBlock.Attributes.Add( "class", "delete block-delete" );
                aDeleteBlock.Attributes.Add( "href", _blockCache.Id.ToString() );
                aDeleteBlock.Attributes.Add( "title", "Delete Block" );
                configControls.Add( aDeleteBlock );
                HtmlGenericControl iDeleteBlock = new HtmlGenericControl( "i" );
                aDeleteBlock.Controls.Add( iDeleteBlock );
                iDeleteBlock.Attributes.Add( "class", "fa fa-times-circle-o" );
            }

            return configControls;
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs.
        /// </summary>
        /// <param name="pageReference">The <see cref="Rock.Web.PageReference"/>.</param>
        /// <returns>A <see cref="System.Collections.Generic.List{BreadCrumb}"/> of block related <see cref="Rock.Web.UI.BreadCrumb">BreadCrumbs</see>.</returns>
        public virtual List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            return new List<BreadCrumb>();
        }

        /// <summary>
        /// Logs an <see cref="System.Exception"/> that has occurred.
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/> to log.</param>
        public void LogException( Exception ex )
        {
            ExceptionLogService.LogException( ex, Context, RockPage.PageId, RockPage.Layout.SiteId, CurrentPersonAlias );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates and or updates any <see cref="Rock.Model.Block"/> <see cref="Rock.Model.Attribute">Attributes</see>.
        /// </summary>
        internal void CreateAttributes( RockContext rockContext )
        {
            int? blockEntityTypeId = EntityTypeCache.Read( typeof( Block ) ).Id;

            if ( Rock.Attribute.Helper.UpdateAttributes( this.GetType(), blockEntityTypeId, "BlockTypeId", this._blockCache.BlockTypeId.ToString(), rockContext ) )
            {
                this._blockCache.ReloadAttributeValues();
            }
        }

        /// <summary>
        /// Reads the <see cref="Rock.Security.SecurityActionAttribute">security action attributes</see> for this <see cref="Rock.Model.Block"/>
        /// </summary>
        /// <returns>A dictionary containing the actions for the <see cref="Rock.Model.Block">Block's</see>
        /// <see cref="Rock.Security.SecurityActionAttribute">SecurityActionAttributes</see>.</returns>
        internal Dictionary<string, string> GetSecurityActionAttributes()
        {
            var securityActions = new Dictionary<string, string>();

            object[] customAttributes = this.GetType().GetCustomAttributes( typeof( SecurityActionAttribute ), true );
            foreach ( var customAttribute in customAttributes )
            {
                var securityActionAttribute = customAttribute as SecurityActionAttribute;
                if (securityActionAttribute != null)
                {
                    securityActions.Add( securityActionAttribute.Action, securityActionAttribute.Description );
                }
            }

            return securityActions;
        }

        /// <summary>
        /// Sets the validation group. If the validationGroup is a prefix to the existingValidationGroup, the existingValidationGroup is returned, 
        /// if the existingValidationGroup name is an empty string, the validationGroup is returned; if the names are different a new validation group is 
        /// created that combines the two.
        /// </summary>
        /// <param name="existingValidationGroup">A <see cref="System.String"/> representing the name of the existing validation group.</param>
        /// <param name="validationGroup">A <see cref="System.String"/> representing the validation group.</param>
        /// <returns>A <see cref="System.String"/> representing the name of the validationGroup.</returns>
        private string SetValidationGroup( string existingValidationGroup, string validationGroup )
        {
            if ( ( existingValidationGroup ?? string.Empty ).StartsWith( validationGroup ) )
            {
                return existingValidationGroup;
            }
            else
            {
                if ( string.IsNullOrWhiteSpace( existingValidationGroup ) )
                {
                    return validationGroup;
                }
                else
                {
                    return string.Format( "{0}_{1}", validationGroup, existingValidationGroup );
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BlockUpdatedEventArgs"/> instance containing the event data.</param>
        internal void Page_BlockUpdated( object sender, BlockUpdatedEventArgs e )
        {
            if ( e.BlockID == _blockCache.Id && BlockUpdated != null )
            {
                BlockUpdated( sender, e );
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the block properties are updated.
        /// </summary>
        public event EventHandler<EventArgs> BlockUpdated;

        #endregion

    }
}

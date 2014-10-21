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
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Web.UI.Controls;

namespace Rock.Field.Types
{
    /// <summary>
    /// Field used to save and display a time value
    /// </summary>
    [Serializable]
    public class TimeFieldType : FieldType
    {
        /// <summary>
        /// Formats time display
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( System.Web.UI.Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            string formattedValue = string.Empty;

            var timeValue = TimeSpan.MinValue;
            if ( TimeSpan.TryParse( value, out timeValue ) )
            {
                formattedValue = timeValue.ToTimeString();
            }

            return base.FormatValue( parentControl, formattedValue, null, condensed );
        }

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            var tp = new TimePicker { ID = id }; 
            return tp;
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( control != null && control is TimePicker )
            {
                var tp = control as TimePicker;
                if ( tp != null && tp.SelectedTime.HasValue )
                {
                    // serialize the time using culture-insensitive "constant" fromat
                    return tp.SelectedTime.Value.ToString("c");
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            var timeValue = TimeSpan.MinValue;
            if ( TimeSpan.TryParse( value, out timeValue ) )
            {
                if ( control != null && control is TimePicker )
                {
                    var tp = control as TimePicker;
                    if ( tp != null )
                    {
                        tp.SelectedTime = timeValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets information about how to configure a filter UI for this type of field. Used primarily for dataviews
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public override Reporting.EntityField GetFilterConfig( Rock.Web.Cache.AttributeCache attribute)
        {
            var filterConfig = base.GetFilterConfig( attribute );
            filterConfig.FilterFieldType = SystemGuid.FieldType.TIME;
            return filterConfig;
        }
    }
}
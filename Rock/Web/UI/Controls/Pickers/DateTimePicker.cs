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
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// control to select a date time
    /// </summary>
    [ToolboxData( "<{0}:DateTimePicker runat=server></{0}:DateTimePicker>" )]
    public class DateTimePicker : CompositeControl, IRockControl
    {
        #region IRockControl implementation

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        /// <value>
        /// The label text.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        DefaultValue( "" ),
        Description( "The text for the label." )
        ]
        public string Label
        {
            get { return ViewState["Label"] as string ?? string.Empty; }
            set { ViewState["Label"] = value; }
        }

        /// <summary>
        /// Gets or sets the help text.
        /// </summary>
        /// <value>
        /// The help text.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        DefaultValue( "" ),
        Description( "The help block." )
        ]
        public string Help
        {
            get
            {
                return HelpBlock != null ? HelpBlock.Text : string.Empty;
            }

            set
            {
                if ( HelpBlock != null )
                {
                    HelpBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RockTextBox"/> is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        [
        Bindable( true ),
        Category( "Behavior" ),
        DefaultValue( "false" ),
        Description( "Is the value required?" )
        ]
        public bool Required
        {
            get 
            {
                EnsureChildControls();
                return _datePicker.Required;
            }
            set 
            {
                EnsureChildControls();
                _datePicker.Required = value;
            }
        }

        /// <summary>
        /// Gets or sets the required error message.  If blank, the LabelName name will be used
        /// </summary>
        /// <value>
        /// The required error message.
        /// </value>
        public string RequiredErrorMessage
        {
            get
            {
                EnsureChildControls();
                return _datePicker.RequiredErrorMessage;
            }

            set
            {
                EnsureChildControls();
                _datePicker.RequiredErrorMessage = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsValid
        {
            get
            {
                EnsureChildControls();
                return _datePicker.IsValid;
            }
        }

        /// <summary>
        /// Gets or sets the help block.
        /// </summary>
        /// <value>
        /// The help block.
        /// </value>
        public HelpBlock HelpBlock { get; set; }

        /// <summary>
        /// Gets or sets the required field validator.
        /// </summary>
        /// <value>
        /// The required field validator.
        /// </value>
        public RequiredFieldValidator RequiredFieldValidator { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimePicker"/> class.
        /// </summary>
        public DateTimePicker()
            : base()
        {
            HelpBlock = new HelpBlock();
        }

        #region Controls

        /// <summary>
        /// The date control part of the date/time picker
        /// </summary>
        private DatePicker _datePicker;

        /// <summary>
        /// The time control part of the date/time picker
        /// </summary>
        private TimePicker _timePicker;

        #endregion

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            Controls.Clear();
            RockControlHelper.CreateChildControls( this, Controls );

            _datePicker = new DatePicker();
            _datePicker.ID = this.ID + "_datePicker";
            _datePicker.CssClass = "input-width-md";

            Controls.Add( _datePicker );

            _timePicker = new TimePicker();
            _timePicker.ID = this.ID + "_timePicker";
            _timePicker.CssClass = "input-width-md";
            Controls.Add( _timePicker );
        }

        /// <summary>
        /// Outputs server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter" /> object and stores tracing information about the control if tracing is enabled.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        /// <summary>
        /// This is where you implment the simple aspects of rendering your control.  The rest
        /// will be handled by calling RenderControlHelper's RenderControl() method.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void RenderBaseControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {

                writer.AddAttribute( "id", this.ClientID );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                writer.AddAttribute( "class", "form-control-group" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                _datePicker.RenderControl( writer );
                writer.WriteLine();
                _timePicker.RenderControl( writer );

                writer.RenderEndTag();

                writer.RenderEndTag();
            }
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string ValidationGroup
        {
            get
            {
                EnsureChildControls();
                return _datePicker.ValidationGroup;
            }

            set
            {
                EnsureChildControls();
                _datePicker.ValidationGroup = value;
                _timePicker.ValidationGroup = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [selected date time is blank].
        /// </summary>
        /// <value>
        /// <c>true</c> if [selected date time is blank]; otherwise, <c>false</c>.
        /// </value>
        public bool SelectedDateTimeIsBlank
        {
            get
            {
                return ( !_datePicker.SelectedDate.HasValue );
            }
        }
        
        /// <summary>
        /// Gets or sets the selected date time.  Defaults to Today if blank
        /// </summary>
        /// <value>
        /// The selected date time.
        /// </value>
        public DateTime? SelectedDateTime
        {
            get
            {
                EnsureChildControls();
                if ( _datePicker.SelectedDate == null && _timePicker.SelectedTime == null)
                {
                    return null;
                }
                return ( _datePicker.SelectedDate ?? RockDateTime.Today ) +
                    ( _timePicker.SelectedTime ?? new TimeSpan( 0 ) );
            }

            set
            {
                EnsureChildControls();
                if ( value.HasValue )
                {
                    _datePicker.SelectedDate = value.Value.Date;
                    _timePicker.SelectedTime = value.Value.TimeOfDay;
                }
                else
                {
                    _datePicker.SelectedDate = null;
                    _timePicker.SelectedTime = null;
                }
            }
        }
    }
}

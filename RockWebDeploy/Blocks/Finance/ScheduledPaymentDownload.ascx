﻿<%@ control language="C#" autoeventwireup="true" inherits="RockWeb.Blocks.Finance.ScheduledPaymentDownload, RockWeb" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">
                
            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-download"></i> 
                    Scheduled Payment Download
                </h1>
            </div>

            <div class="panel-body">

                <Rock:ComponentPicker ID="cpGateway" runat="server" Label="Payment Gateway" Required="true" ContainerType="Rock.Financial.GatewayContainer"  />
                <Rock:DateRangePicker ID="drpDates" runat="server" Label="Date Range" Required="true" />

                <div class="actions">
                    <Rock:BootstrapButton ID="btnDownload" runat="server" CssClass="btn btn-primary" Text="Download Transactions" DataLoadingText="Downloading..." CausesValidation="true" OnClick="btnDownload_Click" />
                </div>
                <br />

                <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success" Heading="Transaction Download Summary:" Visible="false" />
                <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" Visible="false" />

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>

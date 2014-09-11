﻿<%@ control language="C#" autoeventwireup="true" inherits="RockWeb.Blocks.CheckIn.GroupTypeSelect, RockWeb" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <div class="row checkin-header">
            <div class="col-md-12">
                <h1><asp:Literal ID="lPersonName" runat="server" /></h1>
            </div>
        </div>

        <div class="row checkin-body">
            <div class="col-md-12">
                <div class="control-group checkin-body-container">
                    <label class="control-label">Select Area</label>
                    <div class="controls">
                        <asp:Repeater ID="rSelection" runat="server" OnItemCommand="rSelection_ItemCommand">
                            <ItemTemplate>
                                <Rock:BootstrapButton ID="lbSelect" runat="server" Text='<%# Container.DataItem.ToString() %>' CommandArgument='<%# Eval("GroupType.Id") %>' CssClass="btn btn-primary btn-large btn-block btn-checkin-select" DataLoadingText="Loading..." />
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </div>

        <div class="checkin-footer">
            <div class="checkin-actions">
                <asp:LinkButton CssClass="btn btn-default" ID="lbBack" runat="server" OnClick="lbBack_Click" Text="Back" />
                <asp:LinkButton CssClass="btn btn-default" ID="lbCancel" runat="server" OnClick="lbCancel_Click" Text="Cancel" />
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>

# Quick fixes

> Indexed troubleshooting notes captured during implementation and debugging.

## Authentication and routing

- [QF-01 AuthorizeRouteView unauthorized parameter](1_AuthorizeRouteView_Unauthorized_Parameter_Removed_PRESENTATION_ROUTING.md)
- [QF-02 AuthenticationStateProvider circular dependency](2_AuthenticationStateProvider_CircularDependency_DI_INFRASTRUCTURE_AUTH.md)
- [QF-03 Revalidating auth state provider base class](3_RevalidatingIdentityAuthStateProvider_WrongBaseClass_INFRASTRUCTURE_AUTH.md)
- [QF-04 Cascading auth state render-mode boundary](4_CascadingAuthState_RenderModeBoundary_PRESENTATION_COMPONENTS.md)
- [QF-05 EditForm missing FormName](5_EditForm_Missing_FormName_BlazorSSR_PRESENTATION_AUTH.md)
- [QF-06 SupplyParameterFromForm empty model](6_SupplyParameterFromForm_Missing_FormModelEmpty_PRESENTATION_AUTH.md)
- [QF-08 Post-login redirect missing route](8_PostLogin_Redirect_NonExistent_Dashboard_Route_APPLICATION_AUTH.md)
- [QF-09 SignOutAsync no effect in Blazor circuit](9_SignInManager_SignOutAsync_No_Effect_In_Blazor_Circuit_PRESENTATION_AUTH.md)

## Data

- [QF-07 EF Core migrations not applied](7_EFCore_Migrations_NotApplied_AspNetUsers_Missing_INFRASTRUCTURE_DATA.md)

## Security and infrastructure

- [QF-10 CSP blocks CDN fonts and BrowserLink](10_CSP_Blocks_CDN_Fonts_Styles_BrowserLink_INFRASTRUCTURE_SECURITY.md)
- [QF-11 Stripe webhook 307 redirect & 422 ILogger DI failure](11_Stripe_Webhook_307_Redirect_422_ILogger_DI_INFRASTRUCTURE_WEBHOOKS.md)
- [QF-13 Blazor antiforgery rejects API controllers](13_Blazor_Antiforgery_Rejects_API_Controllers_INFRASTRUCTURE_SECURITY.md)
- [QF-18 WDAC blocks unsigned apphost .exe — UseAppHost=false](18_WDAC_Blocks_Apphost_Exe_UseAppHost_False_INFRASTRUCTURE_SETUP.md)

## REST API & Swagger

- [QF-12 ApiAccess policy missing scheme pin](12_ApiAccess_Policy_Missing_Scheme_Pin_INFRASTRUCTURE_AUTH.md)
- [QF-14 Empty ApiKey config — use user-secrets](14_ApiKey_Config_Empty_UseUserSecrets_INFRASTRUCTURE_AUTH.md)
- [QF-15 Swagger global Authorize button for API key](15_Swagger_Global_Authorize_Button_For_ApiKey_PRESENTATION_DOCS.md)

## Payments and data

- [QF-16 EF Core schema drift on ExternalReference NOT NULL](16_EFCore_Schema_Drift_ExternalReference_NotNull_INFRASTRUCTURE_DATA.md)
- [QF-17 Stripe placeholder secret key — use user-secrets](17_Stripe_Placeholder_SecretKey_UserSecrets_INFRASTRUCTURE_PAYMENTS.md)

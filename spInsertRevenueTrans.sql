USE [EnterprisePortal]
GO
/****** Object:  StoredProcedure [dbo].[spInsertRevenueTrans]    Script Date: 3/2/2022 9:18:39 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<tndhliwayo>
-- Description:	<Insert Trans into SQL>
-- =============================================
ALTER PROCEDURE [dbo].[spInsertRevenueTrans]
	-- Add the parameters for the stored procedure here
	@TRN_REF_NO varchar(50),
	@PRODUCT_CODE varchar(50),
	@TXN_CCY varchar(50),
	@USER_ID varchar(50),
	@TXN_AMOUNT decimal(18,2),
	@TXN_CHARGE decimal(18,2),
	@TXN_DATE datetime,
	@CUSTOMER_TYPE varchar(50),
	@CUSTOMER_CATEGORY varchar(50),
	@CHARGE_CODE varchar(50),
	@CHANNEL varchar(50),
	@CODE  varchar(50),
	@CALC_CHARGE decimal(18,2),
	@DATE_ADDED datetime,
	@USERID varchar(50)
AS
DECLARE @Response VARCHAR(50)


IF (SELECT COUNT(*) FROM  tblRevenueTransactions with(nolock) WHERE PRODUCT_CODE = @PRODUCT_CODE AND TRN_REF_NO = @TRN_REF_NO)= 0                            

    BEGIN
        insert into [tblRevenueTransactions] (TRN_REF_NO,PRODUCT_CODE,TXN_CCY,[USER_ID],TXN_AMOUNT,TXN_CHARGE,TXN_DATE,CUSTOMER_TYPE,CUSTOMER_CATEGORY,CHARGE_CODE,CHANNEL,CODE,CALC_CHARGE,DATE_ADDED,USERID)
    values(@TRN_REF_NO,@PRODUCT_CODE,@TXN_CCY,@USER_ID,@TXN_AMOUNT,@TXN_CHARGE,@TXN_DATE,@CUSTOMER_TYPE,@CUSTOMER_CATEGORY,@CHARGE_CODE,@CHANNEL,@CODE,@CALC_CHARGE,GETDATE(),@USERID)
    END

        SELECT ISNULL(SCOPE_IDENTITY(),'0') ID;



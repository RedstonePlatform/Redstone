## COLD STAKING SETUP INSTRUCTIONS

**What you will need** 

 - **Hot wallet** - this will be used for staking online, you can skip this step if you are using **www.trustaking.com**
 - **Cold wallet** - this will hold your funds offline
 
    _NB. It's important that both wallets are running on separate full nodes._

---
****Hot Wallet****

_If you are using www.trustaking.com, the coldstakinghotaddress will be provided for you on the website and you can skip this section._

1. Convert your Hot wallet to be enable cold staking by using the the `cold-staking-account` API method with isColdWalletAccount set to "false"
2. Get your Hot wallets coldstakinghotaddress using by using the `cold-staking-address` API method with "IsColdWalletAddress" set to "false"
3. Then start the node staking with the Hot wallet from the command line or config file.
 
 ---
****Cold wallet****

_If you are using www.trustaking.com, we provide a command to simplify this process._

1. Fund the Cold wallet with coins that you eventually want to stake, these coins should go into the standard "account 0".
2. Convert your Cold wallet to be enabled for cold staking by using the `cold-staking-account` API method with "isColdWalletAccount" set to "true"
3. Get your Cold wallets coldstakingcoldaddress using by using the `cold-staking-address` API method with "IsColdWalletAddress" set to "true"
4. Then, call the `setup-cold-staking` API to build the transaction and send coins from the Hot Wallet Address (Step #2) to Cold Wallet Address (Step #6). This will return the hex that you use in the next step.

```
{
  "coldWalletAddress": "<<coldstakingcoldaddress>>",
  "hotWalletAddress": "<<coldstakinghotaddress>>",
  "walletName": "<<hotwalletname>>",
  "walletPassword": "<<hotwalletpassword>>",
  "walletAccount": "account 0",
  "amount": "<<amount to stake>>",
  "fees": "0.0002"
}
```

5. Finally, you use the "send-transaction" API to broadcast the transaction from step #7.

---

## To withdraw funds back to the cold wallet

_If you are using www.trustaking.com, we provide a command to simplify this process._

1. From the PC running your cold wallet, call the "cold-staking-withdrawal" API to build a transaction and return coins from the Hot Wallet Address to Cold Wallet Address (account 0). This will return the hex that you use in the next step.

```
{
  "receivingAddress": "<<cold wallet address/ account 0>>",
  "walletName": "<<coldwalletname>>",
  "walletPassword": "<<coldwalletpassword>>",
  "amount": "<<amount to to return>>",
  "fees": "0.0001"
}
```

2. Then simply use the "send-transaction" API to broadcast the transaction hex from step #1.

---
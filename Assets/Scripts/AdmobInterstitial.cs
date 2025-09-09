using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;

public class AdmobInterstitial : MonoBehaviour
{
    public string interstitialID = "";

    public InterstitialAd _interstitialAd;

    public static event System.Action OnAdClosed;
    public static event System.Action OnAdFailedToShow;

    public void Start()
    {
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            LoadInterstitialAd();
        });
    }

    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }
        Debug.Log("Loading the interstitial ad.");
        var adRequest = new AdRequest();

        InterstitialAd.Load(interstitialID, adRequest,
          (InterstitialAd ad, LoadAdError error) =>
          {
              if (error != null || ad == null)
              {
                  Debug.LogError("interstitial ad failed to load an ad " +
                      "with error : " + error);
                  return;
              }
              Debug.Log("Interstitial ad loaded with response : "
               + ad.GetResponseInfo());
              _interstitialAd = ad;
              RegisterEventHandlers(_interstitialAd);
          });
    }

    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");

            OnAdFailedToShow?.Invoke();
        }
    }


    public bool IsAdShowing()
    {
        return _interstitialAd != null && _interstitialAd.CanShowAd();
    }

    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {

        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
              adValue.Value,
              adValue.CurrencyCode));
        };


        interstitialAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };


        interstitialAd.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };


        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };


        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");


            OnAdClosed?.Invoke();

            LoadInterstitialAd();
        };


        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                    "with error : " + error);


            OnAdFailedToShow?.Invoke();

            LoadInterstitialAd();
        };
    }


    private void OnDestroy()
    {
        OnAdClosed = null;
        OnAdFailedToShow = null;
    }
}
/**
 * Service for handling MFA challenges in the frontend
 * based on the backend MFA challenge response.
 */

import {
  IPublicClientApplication,
  SilentRequest,
  InteractionRequiredAuthError,
} from '@azure/msal-browser';
import { MfaChallengeError, MfaChallengeDetails } from '@/types';
import { trackAuthEvent } from '@/lib/telemetry';

export interface MfaHandlerOptions {
  msalInstance: IPublicClientApplication;
  account: any;
}

export class MfaHandler {
  private msalInstance: IPublicClientApplication;
  private account: any;

  constructor(options: MfaHandlerOptions) {
    this.msalInstance = options.msalInstance;
    this.account = options.account;
  }

  /**
   * Determines if an error is an MFA challenge that we can handle
   */
  public isMfaChallengeError(error: any): boolean {
    return !!(
      error?.response?.data?.error?.type === 'mfa_required' &&
      error.response.status === 401 &&
      error.config // Ensure it's an axios error with config
    );
  }

  /**
   * Handles an MFA challenge by prompting the user for interactive authentication
   * with the claims challenge from the backend.
   */
  public async handleMfaChallenge(
    challengeDetails: MfaChallengeDetails
  ): Promise<string> {
    if (!this.account) {
      throw new Error('No account available for MFA challenge handling');
    }

    try {
      trackAuthEvent('mfa_challenge_started', {
        correlationId: challengeDetails.correlationId,
        errorCode: challengeDetails.errorCode,
        scopes: challengeDetails.scopes,
      });

      // Create a token request with the claims challenge
      const tokenRequest: SilentRequest = {
        scopes: challengeDetails.scopes,
        account: this.account,
        claims: challengeDetails.claimsChallenge,
        forceRefresh: true, // Force refresh to trigger MFA
      };

      try {
        // First try silent request with claims challenge
        const response =
          await this.msalInstance.acquireTokenSilent(tokenRequest);

        trackAuthEvent('mfa_challenge_completed_silently', {
          correlationId: challengeDetails.correlationId,
        });

        return response.accessToken;
      } catch (silentError) {
        if (silentError instanceof InteractionRequiredAuthError) {
          // Silent acquisition failed, trigger popup/redirect
          trackAuthEvent('mfa_challenge_requires_interaction', {
            correlationId: challengeDetails.correlationId,
            error: silentError.errorCode,
          });

          const popupRequest = {
            scopes: challengeDetails.scopes,
            account: this.account,
            claims: challengeDetails.claimsChallenge,
          };

          const response =
            await this.msalInstance.acquireTokenPopup(popupRequest);

          trackAuthEvent('mfa_challenge_completed_interactive', {
            correlationId: challengeDetails.correlationId,
          });

          return response.accessToken;
        }
        throw silentError;
      }
    } catch (error: any) {
      trackAuthEvent('mfa_challenge_failed', {
        correlationId: challengeDetails.correlationId,
        error: error.message,
      });

      console.error('MFA challenge handling failed:', error);
      throw new Error(`MFA authentication failed: ${error.message}`);
    }
  }

  /**
   * Handles MFA challenge from an API error response
   */
  public async handleMfaChallengeFromError(error: any): Promise<string> {
    if (!this.isMfaChallengeError(error)) {
      throw new Error('Not an MFA challenge error');
    }

    const challengeDetails = error.response.data.error.details;
    return this.handleMfaChallenge(challengeDetails);
  }
}

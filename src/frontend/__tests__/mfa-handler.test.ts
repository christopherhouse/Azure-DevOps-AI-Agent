/**
 * Tests for MFA challenge handling in the frontend
 */

import { MfaHandler } from '@/services/mfa-handler';
import { InteractionRequiredAuthError } from '@azure/msal-browser';

// Mock MSAL types
interface MockMsalInstance {
  acquireTokenSilent: jest.Mock;
  acquireTokenPopup: jest.Mock;
}

interface MockAccount {
  homeAccountId: string;
  username: string;
}

describe('MfaHandler', () => {
  let mockMsalInstance: MockMsalInstance;
  let mockAccount: MockAccount;
  let mfaHandler: MfaHandler;

  beforeEach(() => {
    mockMsalInstance = {
      acquireTokenSilent: jest.fn(),
      acquireTokenPopup: jest.fn(),
    };

    mockAccount = {
      homeAccountId: 'test-account-id',
      username: 'test@example.com',
    };

    mfaHandler = new MfaHandler({
      msalInstance: mockMsalInstance as any,
      account: mockAccount,
    });
  });

  describe('isMfaChallengeError', () => {
    it('should identify MFA challenge errors correctly', () => {
      const mfaError = {
        response: {
          status: 401,
          data: {
            error: {
              type: 'mfa_required',
              details: {
                claimsChallenge: 'test-claims',
                scopes: ['scope1'],
                errorCode: 'AADSTS50079',
              },
            },
          },
        },
        config: {}, // Axios config
      };

      expect(mfaHandler.isMfaChallengeError(mfaError)).toBe(true);
    });

    it('should reject non-MFA errors', () => {
      const regularError = {
        response: {
          status: 500,
          data: {
            error: {
              type: 'server_error',
            },
          },
        },
      };

      expect(mfaHandler.isMfaChallengeError(regularError)).toBe(false);
    });

    it('should reject errors without config (non-axios errors)', () => {
      const mfaError = {
        response: {
          status: 401,
          data: {
            error: {
              type: 'mfa_required',
            },
          },
        },
        // No config property
      };

      expect(mfaHandler.isMfaChallengeError(mfaError)).toBe(false);
    });
  });

  describe('handleMfaChallenge', () => {
    const challengeDetails = {
      claimsChallenge: 'test-claims-challenge',
      scopes: ['https://dev.azure.com/.default'],
      errorCode: 'AADSTS50079',
      correlationId: 'test-correlation',
      classification: 'ConsentRequired',
    };

    it('should handle MFA challenge with silent request success', async () => {
      const expectedToken = 'new-access-token';
      mockMsalInstance.acquireTokenSilent.mockResolvedValue({
        accessToken: expectedToken,
      });

      const result = await mfaHandler.handleMfaChallenge(challengeDetails);

      expect(result).toBe(expectedToken);
      expect(mockMsalInstance.acquireTokenSilent).toHaveBeenCalledWith({
        scopes: challengeDetails.scopes,
        account: mockAccount,
        claims: challengeDetails.claimsChallenge,
        forceRefresh: true,
      });
      expect(mockMsalInstance.acquireTokenPopup).not.toHaveBeenCalled();
    });

    it('should fallback to popup when silent request requires interaction', async () => {
      const expectedToken = 'popup-access-token';
      mockMsalInstance.acquireTokenSilent.mockRejectedValue(
        new InteractionRequiredAuthError('interaction_required')
      );
      mockMsalInstance.acquireTokenPopup.mockResolvedValue({
        accessToken: expectedToken,
      });

      const result = await mfaHandler.handleMfaChallenge(challengeDetails);

      expect(result).toBe(expectedToken);
      expect(mockMsalInstance.acquireTokenPopup).toHaveBeenCalledWith({
        scopes: challengeDetails.scopes,
        account: mockAccount,
        claims: challengeDetails.claimsChallenge,
      });
    });

    it('should throw error when no account is available', async () => {
      const handlerWithoutAccount = new MfaHandler({
        msalInstance: mockMsalInstance as any,
        account: null,
      });

      await expect(
        handlerWithoutAccount.handleMfaChallenge(challengeDetails)
      ).rejects.toThrow('No account available for MFA challenge handling');
    });

    it('should handle popup failure gracefully', async () => {
      const popupError = new Error('Popup blocked');
      mockMsalInstance.acquireTokenSilent.mockRejectedValue(
        new InteractionRequiredAuthError('interaction_required')
      );
      mockMsalInstance.acquireTokenPopup.mockRejectedValue(popupError);

      await expect(
        mfaHandler.handleMfaChallenge(challengeDetails)
      ).rejects.toThrow('MFA authentication failed: Popup blocked');
    });
  });

  describe('handleMfaChallengeFromError', () => {
    it('should extract challenge details from axios error and handle MFA', async () => {
      const expectedToken = 'challenge-token';
      const mfaError = {
        response: {
          status: 401,
          data: {
            error: {
              type: 'mfa_required',
              details: {
                claimsChallenge: 'error-claims',
                scopes: ['error-scope'],
                errorCode: 'AADSTS50079',
                correlationId: 'error-correlation',
                classification: 'ConsentRequired',
              },
            },
          },
        },
        config: {},
      };

      mockMsalInstance.acquireTokenSilent.mockResolvedValue({
        accessToken: expectedToken,
      });

      const result = await mfaHandler.handleMfaChallengeFromError(mfaError);

      expect(result).toBe(expectedToken);
      expect(mockMsalInstance.acquireTokenSilent).toHaveBeenCalledWith({
        scopes: ['error-scope'],
        account: mockAccount,
        claims: 'error-claims',
        forceRefresh: true,
      });
    });

    it('should throw error for non-MFA errors', async () => {
      const regularError = {
        response: {
          status: 500,
          data: { error: { type: 'server_error' } },
        },
      };

      await expect(
        mfaHandler.handleMfaChallengeFromError(regularError)
      ).rejects.toThrow('Not an MFA challenge error');
    });
  });
});
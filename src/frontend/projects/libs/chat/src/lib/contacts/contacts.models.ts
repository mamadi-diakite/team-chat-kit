/** A person as the host describes them to the chat. */
export interface ChatUser {
  readonly id: string;
  readonly displayName: string;
  readonly email: string;
  readonly organizationName: string;
}

/** `user` is the other person: the sender of a received invitation, the recipient of a sent one. */
export interface Invitation {
  readonly id: string;
  readonly user: ChatUser;
  /** Plain text written by the sender. */
  readonly message: string | null;
  readonly createdAtUtc: string;
}

export interface Invitations {
  readonly received: readonly Invitation[];
  readonly sent: readonly Invitation[];
}

export interface Contact {
  readonly user: ChatUser;
  readonly sinceUtc: string;
}

export interface SendInvitationRequest {
  readonly email: string;
  readonly message: string | null;
}

export type InvitationAction = 'accept' | 'decline' | 'cancel';

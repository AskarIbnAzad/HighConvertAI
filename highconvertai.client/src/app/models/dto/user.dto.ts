export interface UserDto {
  id: number;
  fullName: string;
  email: string;
  isEmailVerified: boolean;
  roles: string[];
}

import { Component } from '@angular/core';
import { AuthService } from './services/auth.service';

export interface PageScanResultBase {
  url: string;
  pageType: string;
  issues?: ExtractionIssue[];
}

export interface ExtractionIssue {
  code: string;
  message: string;
  severity: string;
}

export interface HomepageScanResult extends PageScanResultBase {
  homepageExtraction?: {
    hero?: { headline?: string; subheadline?: string; primaryCtaText?: string };
    nav?: { linkCount?: number; hasSearch?: boolean };
    features?: { features?: string[] };
    testimonials?: { hasTestimonials?: boolean; testimonialCount?: number };
    meta?: { confidenceScore?: number };
  };
}

export interface ProductScanResult extends PageScanResultBase {
  productExtraction?: {
    basicInfo?: { productTitle?: string; brand?: string; subtitle?: string };
    pricing?: { currentPrice?: number | null; priceTextRaw?: string };
    cta?: Record<string, unknown>;
    description?: Record<string, unknown>;
    shipping?: Record<string, unknown>;
    trust?: Record<string, unknown>;
    reviews?: Record<string, unknown>;
    faq?: Record<string, unknown>;
    media?: Record<string, unknown>;
  };
}

export type PageScanResult = HomepageScanResult | ProductScanResult;

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.css'],
  standalone: false,
})
export class App {
  constructor(private authService: AuthService) {
    this.authService.restoreSession();
  }
}

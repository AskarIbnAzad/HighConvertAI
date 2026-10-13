import { Component, OnDestroy, ChangeDetectorRef } from '@angular/core';

import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { PageScanService } from '../../services/page-scan.service';
import { AuthService } from '../../services/auth.service';

interface AuditStep {
  key: string;
  title: string;
  description: string;
  status: 'done' | 'running' | 'pending';
  value?: string;
}

@Component({
  selector: 'app-home',
  templateUrl: './home.html',
  styleUrls: ['./home.css'],
  standalone: false,
})
export class HomeComponent implements OnDestroy {
  url: string = '';

  urlError: string = '';

  loading = false;

  progress = 0;

  detectedSiteType = 'Detecting...';

  apiCompleted = false;

  apiResult: any = null;

  auditId: number = 0;

  result: any = null;

  openFaqIndex: number | null = null;

  validationTitle = '';

  validationMessage = '';

  validationFooter = '';

  validationButton = '';

  showValidationCard = false;

  private readonly SCREENSHOT_BASE = 'https://localhost:7115/api/screenshot';

  tips: string[] = [
    'Clear CTA buttons usually increase conversion rates.',
    'Trust badges near pricing can improve purchase confidence.',
    'Product descriptions with benefits outperform feature-only copy.',
    'Fast-loading pages reduce abandonment significantly.',
  ];

  currentTipIndex = 0;

  steps: AuditStep[] = this.freshSteps();

  private progressTimer: any;

  private tipTimer: any;

  faqs = [
    {
      question: 'How accurate is this CRO audit?',
      answer:
        'The audit uses AI analysis and conversion best practices to identify likely CRO and UX issues.',
    },
    {
      question: 'Does this work for Shopify stores?',
      answer: 'Yes. The system is optimized for Shopify product and landing pages.',
    },
    {
      question: 'Can this improve conversions?',
      answer:
        'The audit identifies conversion blockers and optimization opportunities that may help improve sales.',
    },
    {
      question: 'Is this audit free?',
      answer: 'Yes. The instant audit is completely free.',
    },
  ];

  constructor(
    private router: Router,
    private pageScanService: PageScanService,
    private cdr: ChangeDetectorRef,
    private authService: AuthService,
  ) {}

  validationAction(): void {
    switch (this.validationButton) {
      // ==========================
      // Guest
      // ==========================
      case 'Create Free Account':
        this.router.navigate(['/register']);
        break;

      // ==========================
      // Logged in user
      // ==========================
      case 'Contact High Convert Support':
        window.location.href = 'mailto:support@highconvert.ai';
        break;

      // ==========================
      // AI Busy
      // ==========================
      case 'Try Again':
        this.showValidationCard = false;
        this.analyze();
        break;

      // ==========================
      // Website Error
      // ==========================
      case 'Return to Audit':
        this.showValidationCard = false;

        setTimeout(() => {
          const input = document.querySelector('.input-wrap input') as HTMLInputElement;

          input?.focus();

          window.scrollTo({
            top: 0,
            behavior: 'smooth',
          });
        });

        break;

      // ==========================
      // Default
      // ==========================
      default:
        this.showValidationCard = false;
        break;
    }
  }
  /* =========================================
     FAQ
  ========================================= */

  toggleFaq(index: number): void {
    if (this.openFaqIndex === index) {
      this.openFaqIndex = null;
    } else {
      this.openFaqIndex = index;
    }
  }

  /* =========================================
     SCROLL TO AUDIT
  ========================================= */

  scrollToAudit(): void {
    const element = document.getElementById('audit-section');

    if (element) {
      element.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    }
  }

  /* =========================================
     STEP TEMPLATE
  ========================================= */

  private freshSteps(): AuditStep[] {
    return [
      {
        key: 'connection',
        title: 'Connection Established',
        description: 'Connecting securely to the target website',
        status: 'pending',
        value: 'Pending',
      },

      {
        key: 'initialize',
        title: 'Initializing Audit',
        description: 'Preparing AI audit systems and page analysis',
        status: 'pending',
        value: 'Pending',
      },

      {
        key: 'checks',
        title: 'Running CRO Checks',
        description: 'Analyzing trust, CTA, layout, messaging and UX',
        status: 'pending',
        value: 'Pending',
      },

      {
        key: 'recommendations',
        title: 'Generating Recommendations',
        description: 'Preparing AI-powered CRO suggestions',
        status: 'pending',
        value: 'Pending',
      },
    ];
  }

  /* =========================================
     UPDATE STEP
  ========================================= */

  private setStep(key: string, status: AuditStep['status'], value: string): void {
    this.steps = this.steps.map((step) =>
      step.key === key
        ? {
            ...step,
            status,
            value,
          }
        : step,
    );
  }

  analyzeWithDummyData(): void {
    if (!this.url || this.url.trim() === '') {
      return;
    }

    const mockResponse = {
      scanResult: {
        url: 'https://twelvebd.com/products/ph-panp-tm24-04f-48080-chamomile',
        pageType: 'Product',
      },

      analysis: {
        summary:
          'The product page contains multiple CRO, UX, trust, and conversion optimization opportunities related to content quality, social proof, checkout clarity, visual hierarchy, and mobile usability.',

        items: [],

        groups: [
          // =====================================================
          // 1. CONTENT COMPLETENESS
          // =====================================================

          {
            groupType: 'Content Comp.',

            issueCount: 2,

            highCount: 0,

            mediumCount: 2,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 4,

            condition: 'Average',

            items: [
              {
                id: 'missing-product-description',

                groupType: 'Content Comp.',

                severity: 'Medium',

                title: 'Product Description is Missing',

                whatWasRight: 'The product information section exists and is visible.',

                whatToImprove:
                  'The description lacks detailed information buyers need before purchasing.',

                recommendation: 'Add sizing, fabric, fit, care instructions, and styling details.',
              },

              {
                id: 'missing-size-chart',

                groupType: 'Content Comp.',

                severity: 'Medium',

                title: 'Size Guide is Not Clearly Visible',

                whatWasRight: 'Product variants and sizing options are present.',

                whatToImprove: 'Users may hesitate to purchase due to sizing uncertainty.',

                recommendation: 'Add a visible size chart near the add-to-cart section.',
              },
            ],
          },

          // =====================================================
          // 2. TRUST SIGNALS
          // =====================================================

          {
            groupType: 'Trust Signals',

            issueCount: 2,

            highCount: 1,

            mediumCount: 1,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 5,

            condition: 'Critical',

            items: [
              {
                id: 'missing-reviews',

                groupType: 'Trust Signals',

                severity: 'High',

                title: 'Customer Reviews are Missing',

                whatWasRight: 'The layout has space reserved for customer feedback.',

                whatToImprove: 'Missing reviews reduce buyer trust and product credibility.',

                recommendation: 'Add verified customer reviews and star ratings.',
              },

              {
                id: 'no-security-badge',

                groupType: 'Trust Signals',

                severity: 'Medium',

                title: 'Checkout Security Signals are Weak',

                whatWasRight: 'The checkout flow appears functional.',

                whatToImprove: 'Users may not immediately feel payment confidence.',

                recommendation: 'Display SSL, secure payment, and refund badges.',
              },
            ],
          },

          // =====================================================
          // 3. PERFORMANCE
          // =====================================================

          {
            groupType: 'Performance',

            issueCount: 2,

            highCount: 1,

            mediumCount: 1,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 4,

            condition: 'Critical',

            items: [
              {
                id: 'slow-lcp',

                groupType: 'Performance',

                severity: 'High',

                title: 'Large Contentful Paint is Slow',

                whatWasRight: 'The page assets load successfully.',

                whatToImprove: 'The first visible content takes too long to appear.',

                recommendation: 'Optimize hero images and reduce render-blocking resources.',
              },

              {
                id: 'heavy-images',

                groupType: 'Performance',

                severity: 'Medium',

                title: 'Images are Not Optimized',

                whatWasRight: 'Product imagery quality is visually appealing.',

                whatToImprove: 'Large image sizes may slow mobile performance.',

                recommendation: 'Use WebP compression and lazy loading.',
              },
            ],
          },

          // =====================================================
          // 4. MOBILE UX
          // =====================================================

          {
            groupType: 'Mobile UX',

            issueCount: 2,

            highCount: 0,

            mediumCount: 2,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 3,

            condition: 'Average',

            items: [
              {
                id: 'cta-spacing',

                groupType: 'Mobile UX',

                severity: 'Medium',

                title: 'CTA Buttons are Too Close Together',

                whatWasRight: 'Primary actions are visible on mobile devices.',

                whatToImprove: 'Touch targets may feel crowded on smaller screens.',

                recommendation: 'Increase spacing and tap area around CTA buttons.',
              },

              {
                id: 'sticky-cart-missing',

                groupType: 'Mobile UX',

                severity: 'Medium',

                title: 'Sticky Add-to-Cart is Missing',

                whatWasRight: 'The add-to-cart section exists clearly.',

                whatToImprove: 'Users may need to scroll repeatedly to purchase.',

                recommendation: 'Add a sticky mobile add-to-cart bar.',
              },
            ],
          },

          // =====================================================
          // 5. VISUAL HIERARCHY
          // =====================================================

          {
            groupType: 'Visual Hierarchy',

            issueCount: 1,

            highCount: 0,

            mediumCount: 1,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 2,

            condition: 'Average',

            items: [
              {
                id: 'weak-cta-contrast',

                groupType: 'Visual Hierarchy',

                severity: 'Medium',

                title: 'Primary CTA Does Not Stand Out Enough',

                whatWasRight: 'The CTA button is placed above the fold.',

                whatToImprove: 'The visual contrast is not strong enough to attract attention.',

                recommendation: 'Increase CTA contrast and visual prominence.',
              },
            ],
          },

          // =====================================================
          // 6. SOCIAL PROOF
          // =====================================================

          {
            groupType: 'Social Proof',

            issueCount: 2,

            highCount: 1,

            mediumCount: 1,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 5,

            condition: 'Critical',

            items: [
              {
                id: 'ugc-missing',

                groupType: 'Social Proof',

                severity: 'High',

                title: 'User Generated Content is Missing',

                whatWasRight: 'The product imagery is professionally designed.',

                whatToImprove: 'The page lacks authentic customer lifestyle visuals.',

                recommendation: 'Add customer uploaded photos and testimonials.',
              },

              {
                id: 'low-review-count',

                groupType: 'Social Proof',

                severity: 'Medium',

                title: 'Low Quantity of Visible Reviews',

                whatWasRight: 'Some trust indicators are available.',

                whatToImprove: 'Review volume appears insufficient for confidence building.',

                recommendation: 'Increase review collection campaigns after purchase.',
              },
            ],
          },

          // =====================================================
          // 7. CHECKOUT EXPERIENCE
          // =====================================================

          {
            groupType: 'Checkout UX',

            issueCount: 2,

            highCount: 1,

            mediumCount: 1,

            lowCount: 0,

            estimatedRevenueIncreasePercent: 6,

            condition: 'Critical',

            items: [
              {
                id: 'shipping-clarity',

                groupType: 'Checkout UX',

                severity: 'High',

                title: 'Shipping Information is Not Clear',

                whatWasRight: 'Delivery options are technically available.',

                whatToImprove: 'Users may abandon cart due to uncertainty around delivery timing.',

                recommendation: 'Show estimated delivery dates near CTA buttons.',
              },

              {
                id: 'return-policy-hidden',

                groupType: 'Checkout UX',

                severity: 'Medium',

                title: 'Return Policy Visibility is Weak',

                whatWasRight: 'A refund policy exists on the website.',

                whatToImprove: 'The policy is not visible enough during buying decisions.',

                recommendation: 'Add concise return policy highlights near checkout.',
              },
            ],
          },

          // =====================================================
          // 8. SEO & DISCOVERABILITY
          // =====================================================

          {
            groupType: 'SEO',

            issueCount: 2,

            highCount: 0,

            mediumCount: 1,

            lowCount: 1,

            estimatedRevenueIncreasePercent: 2,

            condition: 'Average',

            items: [
              {
                id: 'meta-description',

                groupType: 'SEO',

                severity: 'Medium',

                title: 'Meta Description Needs Improvement',

                whatWasRight: 'The page has a title and basic metadata.',

                whatToImprove: 'The description is not optimized for CTR.',

                recommendation: 'Write a keyword-rich and conversion-focused meta description.',
              },

              {
                id: 'alt-tags',

                groupType: 'SEO',

                severity: 'Low',

                title: 'Image Alt Tags are Missing',

                whatWasRight: 'Images are properly displayed across the page.',

                whatToImprove: 'Search engines cannot fully understand the imagery.',

                recommendation: 'Add descriptive alt tags to all product images.',
              },
            ],
          },
        ],

        stats: {
          passedTests: 9,

          issueCount: 15,

          highCount: 4,

          mediumCount: 10,

          lowCount: 1,

          totalChecks: 24,

          score: 72,

          estimatedRevenueIncreasePercent: 14,
        },
      },
    };

    this.auditId = 4;
    this.router.navigate(['/result', this.auditId]);
  }

  analyzeWithRealData(): void {
    if (!this.url || this.url.trim() === '') {
      return;
    }

    this.startLoader();

    this.pageScanService.scanAndAnalyze(this.url).subscribe({
      next: (res) => {
        console.log('SCAN RESULT', res);
        this.apiCompleted = true;
        this.auditId = res.auditId;
        this.cdr.detectChanges();
      },

      error: (err) => {
        console.error(err);
        clearInterval(this.progressTimer);
        clearInterval(this.tipTimer);
        this.loading = false;
        this.cdr.detectChanges();
        const message =
          err?.error?.message ||
          err?.error?.title ||
          err?.message ||
          'Something went wrong. Please try again.';
        debugger;
        const apiError = err?.error;

        this.showValidationCard = true;
        this.loading = false;

        switch (err.status) {
          // =============================
          // Validation (403)
          // =============================
          case 403:
            this.validationTitle = apiError?.title || 'Audit Limit Reached';

            this.validationMessage =
              apiError?.message || 'You cannot run another audit at this time.';

            this.validationFooter = this.authService.isLoggedIn()
              ? 'Need additional audits? Contact High Convert Support.'
              : 'Create a free account to unlock additional audits and save your reports.';

            this.validationButton = this.authService.isLoggedIn()
              ? 'Contact High Convert Support'
              : 'Create Free Account';

            break;

          // =============================
          // AI Busy
          // =============================
          case 503:
            this.validationTitle = 'AI Service Busy';

            this.validationMessage = 'Our AI service is temporarily experiencing high demand.';

            this.validationFooter = 'Please wait a few minutes and try your audit again.';

            this.validationButton = 'Try Again';

            break;

          // =============================
          // Bad URL
          // =============================
          case 400:
            this.validationTitle = 'Invalid Website';

            this.validationMessage = apiError?.message || 'Please enter a valid website URL.';

            this.validationFooter = 'Correct the URL and try running the audit again.';

            this.validationButton = 'Return to Audit';

            break;

          // =============================
          // Everything else
          // =============================
          default:
            this.validationTitle = 'Audit Failed';

            this.validationMessage =
              apiError?.message || 'The website appears to be unavailable or unreachable.';

            this.validationFooter = 'Please try again or contact support if the problem persists.';

            this.validationButton = 'Return to Audit';

            break;
        }

        this.cdr.detectChanges();

        setTimeout(() => {
          window.scrollTo({
            top: 0,
            behavior: 'smooth',
          });
        });
      },
    });
  }

  private validateUrl(): boolean {
    const value = this.url?.trim() ?? '';

    if (!value) {
      this.urlError = 'Please enter your website URL.';
      return false;
    }

    let parsedUrl: URL;

    try {
      parsedUrl = new URL(value);
    } catch {
      this.urlError = 'Please enter a valid website URL, for example https://www.example.com.';
      return false;
    }

    if (!['http:', 'https:'].includes(parsedUrl.protocol)) {
      this.urlError = 'Please enter a valid website URL starting with https://.';
      return false;
    }

    if (!parsedUrl.hostname || !parsedUrl.hostname.includes('.')) {
      this.urlError = 'Please enter a valid website URL, for example https://www.example.com.';
      return false;
    }

    this.urlError = '';
    return true;
  }

  clearUrlError(): void {
    if (this.urlError) {
      this.urlError = '';
    }
  }
  /* =========================================
     ANALYZE
  ========================================= */

  // analyze(): void {
  //   this.showValidationCard = false;

  //   this.validationTitle = '';

  //   this.validationMessage = '';

  //   this.validationFooter = '';

  //   this.validationButton = '';

  //   this.prewarmScreenshots(this.url);

  //   this.analyzeWithRealData();
  //   this.prewarmScreenshots(this.url);

  //   this.analyzeWithRealData();
  //   this.analyzeWithDummyData();
  // }

  analyze(): void {
    this.showValidationCard = false;

    this.validationTitle = '';
    this.validationMessage = '';
    this.validationFooter = '';
    this.validationButton = '';

    if (!this.validateUrl()) {
      return;
    }

    const cleanUrl = this.url.trim();
    this.url = cleanUrl;

    this.prewarmScreenshots(cleanUrl);

    this.analyzeWithRealData();
  }

  private prewarmScreenshots(url: string): void {
    const encoded = encodeURIComponent(url);
    const base = this.SCREENSHOT_BASE;

    // Both requests fire simultaneously — we don't await or care about response
    // The browser/server caches the result so result page gets it instantly
    fetch(`${base}/desktop?url=${encoded}`, { mode: 'no-cors' }).catch(() => {});
    fetch(`${base}/mobile?url=${encoded}`, { mode: 'no-cors' }).catch(() => {});
  }

  /* =========================================
     LOADER
  ========================================= */

  private startLoader(): void {
    this.loading = true;

    this.progress = 0;

    this.currentTipIndex = 0;

    this.detectedSiteType = 'Detecting...';

    this.apiCompleted = false;

    this.auditId = 0;

    this.steps = this.freshSteps();

    clearInterval(this.tipTimer);

    clearInterval(this.progressTimer);

    /* ROTATE TIPS */

    this.tipTimer = setInterval(() => {
      this.currentTipIndex = (this.currentTipIndex + 1) % this.tips.length;

      this.cdr.detectChanges();
    }, 2200);

    const TICK_MS = 300;

    const getIncrement = (p: number): number => {
      if (p < 20) return 3.5;

      if (p < 40) return 2.5;

      if (p < 60) return 1.8;

      if (p < 75) return 1.2;

      if (p < 85) return 0.7;

      if (p < 92) return 0.3;

      return 0.1;
    };

    this.progressTimer = setInterval(() => {
      /* STEP STATES */

      if (this.progress >= 5 && this.steps[0].status === 'pending') {
        this.setStep('connection', 'done', 'Done');
      }

      if (this.progress >= 18 && this.steps[1].status === 'pending') {
        this.setStep('initialize', 'done', 'Done');

        this.detectedSiteType = 'Product Page';
      }

      if (this.progress >= 28 && this.steps[2].status === 'pending') {
        this.setStep('checks', 'running', '0%');
      }

      /* LIVE CHECK % */

      if (this.steps[2].status === 'running' && this.progress < 95) {
        const pct = Math.min(100, Math.round(((this.progress - 28) / (95 - 28)) * 100));

        this.setStep('checks', 'running', `${pct}%`);
      }

      /* PHASE A */

      if (this.progress < 95) {
        this.progress = Math.min(95, this.progress + getIncrement(this.progress));

        this.cdr.detectChanges();

        return;
      }

      /* PHASE B */

      if (!this.apiCompleted) {
        this.progress = 95 + Math.random() * 0.4;

        this.cdr.detectChanges();

        return;
      }

      /* PHASE C */

      if (this.steps[2].status === 'running') {
        this.setStep('checks', 'done', 'Done');

        this.setStep('recommendations', 'running', 'Running');
      }

      this.progress = Math.min(100, this.progress + 2.5);

      this.cdr.detectChanges();

      if (this.progress >= 100) {
        this.progress = 100;

        this.setStep('recommendations', 'done', 'Done');

        this.cdr.detectChanges();

        clearInterval(this.progressTimer);

        clearInterval(this.tipTimer);

        setTimeout(() => {
          this.loading = false;
          this.router.navigate(['/result', this.auditId]);
        }, 500);
      }
    }, TICK_MS);
  }

  /* =========================================
     DESTROY
  ========================================= */

  ngOnDestroy(): void {
    clearInterval(this.progressTimer);

    clearInterval(this.tipTimer);
  }
}

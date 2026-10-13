import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';

export interface CroCheckRequest {
    url: string;
    pageType: 'homepage' | 'landing' | 'product';
    targetRegion: 'global' | 'us' | 'eu' | 'apac';
}

export interface CroInsight {
    title: string;
    detail: string;
    impact: 'High' | 'Medium' | 'Low';
}

export interface CroCheckResult {
    score: number;
    readiness: 'Strong' | 'Promising' | 'Needs Work';
    quickWins: string[];
    risks: string[];
    insights: CroInsight[];
}

@Injectable({
    providedIn: 'root'
})
export class CroAnalyzerService {
    analyze(request: CroCheckRequest): Observable<CroCheckResult> {
        const url = request.url.toLowerCase();

        const scoreBase = request.pageType === 'landing' ? 68 : request.pageType === 'homepage' ? 72 : 64;
        const scoreBoost = url.includes('shop') ? 8 : url.includes('app') ? 6 : 3;
        const scorePenalty = url.includes('beta') ? 7 : 0;
        const score = Math.max(35, Math.min(97, scoreBase + scoreBoost - scorePenalty));

        const readiness: CroCheckResult['readiness'] =
            score >= 80 ? 'Strong' : score >= 65 ? 'Promising' : 'Needs Work';

        const quickWins = [
            'Add a clearer value proposition above the fold.',
            'Place one primary CTA in hero and one in sticky navigation.',
            'Include social proof badges near the first CTA.'
        ];

        const risks = [
            'Too many competing actions can reduce click-through rate.',
            'Weak headline specificity lowers visitor confidence.',
            'Form friction may hurt lead capture for mobile traffic.'
        ];

        const insights: CroInsight[] = [
            {
                title: 'Message Match',
                detail: `Your ${request.pageType} page should align ad intent with hero copy in the first 3 seconds.`,
                impact: 'High'
            },
            {
                title: 'Trust Surface',
                detail: 'Feature logos, user quotes, and guarantees before long-form sections.',
                impact: 'Medium'
            },
            {
                title: 'Mobile CTA Reachability',
                detail: 'Keep primary CTA in thumb zone and reduce form fields for smaller screens.',
                impact: 'High'
            }
        ];

        return of({ score, readiness, quickWins, risks, insights }).pipe(delay(950));
    }
}

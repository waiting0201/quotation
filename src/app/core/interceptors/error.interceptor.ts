import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status >= 400 && err.status < 500 && err.status !== 401) {
        const message =
          err.error?.error?.message ?? `請求失敗 (${err.status})`;
        notify.error(message);
      } else if (err.status >= 500) {
        notify.error('伺服器發生錯誤，請稍後再試');
      }
      return throwError(() => err);
    })
  );
};

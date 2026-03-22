import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';
class MT5SuccessScreen extends StatelessWidget {
  const MT5SuccessScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: FinanceAppBar(
        title: 'Confirmation',
        leading: Container(
          margin: const EdgeInsets.all(8),
          decoration: const BoxDecoration(
            color: AppColors.primary,
            shape: BoxShape.circle,
          ),
          alignment: Alignment.center,
          child: const Text('W',
              style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                  fontSize: 16)),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            const SizedBox(height: 12),
            const CoinAnimation(),
            const SizedBox(height: 10),
            const Text('Transfer Successful!',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary)),
            const SizedBox(height: 6),
            const Text(
              'Your money has been transferred successfully.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 13, color: AppColors.textMuted),
            ),
            const SizedBox(height: 20),
            const AvatarCircle(initials: 'A', size: 56),
            const SizedBox(height: 8),
            const Text('Ahmed',
                style: TextStyle(
                    fontSize: 15, fontWeight: FontWeight.w700, color: AppColors.textDark)),
            const SizedBox(height: 4),
            const Text('p••••••p333',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
            const SizedBox(height: 10),
            const StatusBadge(status: TransactionStatus.sent),
            const SizedBox(height: 14),
            RichText(
              text: const TextSpan(
                children: [
                  TextSpan(
                    text: '\$250.00',
                    style: TextStyle(
                        fontSize: 30,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textDark),
                  ),
                  TextSpan(
                    text: ' USD',
                    style: TextStyle(fontSize: 14, color: AppColors.textMuted),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              decoration: BoxDecoration(
                  color: AppColors.inputBg,
                  borderRadius: BorderRadius.circular(12)),
              child: const Column(
                children: [
                  InfoRow(label: 'Card Type', value: 'Debit Card'),
                  InfoRow(label: 'Transfer Fee', value: '\$0.00 USD'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              text: 'View Receipt',
              onPressed: () => showModalBottomSheet(
                context: context,
                isScrollControlled: true,
                backgroundColor: Colors.transparent,
                builder: (_) => Container(
                  decoration: const BoxDecoration(
                    color: AppColors.cardBg,
                    borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
                  ),
                  padding: const EdgeInsets.fromLTRB(18, 12, 18, 24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 40,
                        height: 4,
                        decoration: BoxDecoration(
                          color: AppColors.border,
                          borderRadius: BorderRadius.circular(2),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: AppColors.inputBg,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [
                            const AvatarCircle(initials: 'A', size: 50),
                            const SizedBox(height: 8),
                            const Text('Ahmed',
                                style: TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 15)),
                            const Text('p••••••p333',
                                style: TextStyle(
                                    fontSize: 11,
                                    color: AppColors.textMuted)),
                            const SizedBox(height: 10),
                            const StatusBadge(status: TransactionStatus.sent),
                            const SizedBox(height: 10),
                            RichText(
                              textAlign: TextAlign.center,
                              text: const TextSpan(
                                children: [
                                  TextSpan(
                                    text: '\$250.00',
                                    style: TextStyle(
                                        fontSize: 28,
                                        fontWeight: FontWeight.w800,
                                        color: AppColors.textDark),
                                  ),
                                  TextSpan(
                                    text: ' USD',
                                    style: TextStyle(
                                        fontSize: 13,
                                        color: AppColors.textMuted),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 12),
                            const Divider(),
                            const InfoRow(label: 'Card Type', value: 'Debit Card'),
                            const InfoRow(label: 'Transfer Fee', value: '\$0.00'),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      PrimaryButton(
                        text: 'Back to Home',
                        onPressed: () =>
                            Navigator.popUntil(context, (r) => r.isFirst),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
